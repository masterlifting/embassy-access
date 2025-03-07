module EA.Telegram.Services.Culture.Command

open Infrastructure.Prelude
open Multilang.Domain
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer

let setCulture (culture: Culture) =
    fun (deps: Culture.Dependencies) ->
        culture
        |> deps.setCurrentCulture
        |> ResultAsync.map (fun _ -> (deps.ChatId, Replace deps.MessageId) |> Text.create $"Culture set to {culture}")

let setCultureCallback (culture: Culture) =
    fun (deps: Culture.Dependencies) -> culture |> deps.setCurrentCulture

let private translateButtonsGroup culture (payload: Payload<ButtonsGroup>) =
    let group = payload.Value

    let items =
        { Id = group.Name; Value = group.Name }
        :: (group.Buttons
            |> Set.map (fun button ->
                { Id = button.Callback.Value
                  Value = button.Name })
            |> Set.toList)

    let request = { Culture = culture; Items = items }

    request
    |> Multilang.Translator.translate
    |> ResultAsync.map (fun response ->
        let responseItemsMap =
            response.Items |> List.map (fun item -> item.Id, item.Value) |> Map.ofList

        let buttonsGroupName =
            responseItemsMap |> Map.tryFind group.Name |> Option.defaultValue group.Name

        let buttons =
            group.Buttons
            |> Set.map (fun button ->
                let buttonName =
                    responseItemsMap
                    |> Map.tryFind button.Callback.Value
                    |> Option.defaultValue button.Name

                { button with Name = buttonName })

        { group with
            Name = buttonsGroupName
            Buttons = buttons })
    |> ResultAsync.map (fun value -> { payload with Value = value } |> ButtonsGroup)

let private translateText culture (payload: Payload<string>) =
    let id = "0"
    let text = payload.Value

    let request =
        { Culture = culture
          Items = [ { Id = id; Value = text } ] }

    request
    |> Multilang.Translator.translate
    |> ResultAsync.map (fun response ->
        response.Items
        |> List.map (fun item -> item.Id, item.Value)
        |> Map.ofList
        |> Map.tryFind id
        |> Option.defaultValue text)
    |> ResultAsync.map (fun value -> { payload with Value = value } |> Text)

let translate culture message =
    fun (deps: Culture.Dependencies) ->
        match message with
        | Text payload -> payload |> translateText culture
        | ButtonsGroup payload -> payload |> translateButtonsGroup culture

let translateSeq culture messages =
    fun (deps: Culture.Dependencies) ->
        messages
        |> List.map (fun message -> deps |> translate culture message)
        |> Async.Sequential
        |> Async.map Result.choose

let translateRes culture msgRes =
    fun (deps: Culture.Dependencies) ->
        msgRes
        |> ResultAsync.bindAsync (fun message -> deps |> translate culture message)

let translateSeqRes culture msgRes =
    fun (deps: Culture.Dependencies) ->
        msgRes
        |> ResultAsync.bindAsync (fun messages -> deps |> translateSeq culture messages)
