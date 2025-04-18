[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services
open AIProvider.Services.Domain
open Web.Clients.Domain.Telegram
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.DataAccess

module private Payload =
    module Error =
        let translate culture (error: Error') =
            fun (translate, shield) ->
                let text = error.Message

                let request = {
                    Culture = culture
                    Shield = shield
                    Items = [ { Value = text } ]
                }

                translate request
                |> ResultAsync.map (fun response ->
                    response.Items
                    |> List.map (fun item -> item.Value, item.Result |> Option.defaultValue item.Value)
                    |> Map.ofList
                    |> Map.tryFind text
                    |> Option.defaultValue text)
                |> ResultAsync.map error.Replace

    module Text =
        let translate culture (payload: Payload<string>) =
            fun (translate, shield) ->
                let text = payload.Value

                let request = {
                    Culture = culture
                    Shield = shield
                    Items = [ { Value = text } ]
                }

                translate request
                |> ResultAsync.map (fun response ->
                    response.Items
                    |> List.map (fun item -> item.Value, item.Result |> Option.defaultValue item.Value)
                    |> Map.ofList
                    |> Map.tryFind text
                    |> Option.defaultValue text)
                |> ResultAsync.map (fun value -> { payload with Value = value } |> Text)

    module ButtonsGroup =
        let translate culture (payload: Payload<ButtonsGroup>) =
            fun (translate, shield) ->
                let group = payload.Value

                let items =
                    { Value = group.Name }
                    :: (group.Buttons |> Set.map (fun button -> { Value = button.Name }) |> Set.toList)

                let request = {
                    Culture = culture
                    Shield = shield
                    Items = items
                }

                translate request
                |> ResultAsync.map (fun response ->
                    let responseItemsMap =
                        response.Items
                        |> List.map (fun item -> item.Value, item.Result |> Option.defaultValue item.Value)
                        |> Map.ofList

                    let buttonsGroupName =
                        responseItemsMap |> Map.tryFind group.Name |> Option.defaultValue group.Name

                    let buttons =
                        group.Buttons
                        |> Seq.map (fun button ->
                            let buttonName =
                                responseItemsMap |> Map.tryFind button.Name |> Option.defaultValue button.Name

                            { button with Name = buttonName })
                        |> Seq.sortBy _.Name
                        |> Set.ofSeq

                    {
                        group with
                            Name = buttonsGroupName
                            Buttons = buttons
                    })
                |> ResultAsync.map (fun value -> { payload with Value = value } |> ButtonsGroup)

type Dependencies = {
    ChatId: ChatId
    MessageId: int
    getAvailable: unit -> Map<Culture, string>
    setCurrent: Culture -> Async<Result<unit, Error'>>
    translate: Culture -> Message -> Async<Result<Message, Error'>>
    translateSeq: Culture -> Message seq -> Async<Result<Message list, Error'>>
    translateRes: Culture -> Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
    translateSeqRes: Culture -> Async<Result<Message seq, Error'>> -> Async<Result<Message list, Error'>>
} with

    static member create(deps: Request.Dependencies) =

        let getAvailable () =
            [
                English, "English"
                Russian, "Русский"
                Chinese, "中文"
                Spanish, "Español"
                Hindi, "हिन्दी"
                Arabic, "العربية"
                Serbian, "Српски"
                Portuguese, "Português"
                French, "Français"
                German, "Deutsch"
                Japanese, "日本語"
                Korean, "한국어"
            ]
            |> Map

        let setCurrent culture =
            deps.Client.Persistence.initChatStorage ()
            |> ResultAsync.wrap (Storage.Chat.Command.setCulture deps.ChatId culture)

        let shield = Shield.create ''' '''

        let translate request =
            deps.Client.Culture |> Culture.translate request deps.ct

        let translateError culture error =
            (translate, shield)
            |> Payload.Error.translate culture error
            |> Async.map (function
                | Ok error -> error
                | Error error -> error)

        let translate culture message =
            match message with
            | Text payload -> (translate, shield) |> Payload.Text.translate culture payload
            | ButtonsGroup payload -> (translate, shield) |> Payload.ButtonsGroup.translate culture payload

        let translateSeq culture messages =
            messages
            |> Seq.map (translate culture)
            |> Async.Sequential
            |> Async.map Result.choose

        let translateRes culture msgRes =
            msgRes
            |> ResultAsync.bindAsync (translate culture)
            |> ResultAsync.mapErrorAsync (translateError culture)

        let translateSeqRes culture msgSeqRes =
            msgSeqRes
            |> ResultAsync.bindAsync (translateSeq culture)
            |> ResultAsync.mapErrorAsync (translateError culture)

        {
            ChatId = deps.ChatId
            MessageId = deps.MessageId
            getAvailable = getAvailable
            setCurrent = setCurrent
            translate = translate
            translateSeq = translateSeq
            translateRes = translateRes
            translateSeqRes = translateSeqRes
        }
