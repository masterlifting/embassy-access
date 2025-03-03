[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Culture.Culture

open Infrastructure.Domain
open Infrastructure.Prelude
open Multilang
open Multilang.Domain
open Web.Telegram.Domain.Producer
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Endpoints.Culture.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Culture

let respond request entrypoint =
    fun (consumerDeps: Consumer.Dependencies) ->
        Culture.Dependencies.create consumerDeps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.Cultures -> deps |> Query.getCultures () |> deps.sendResult
            | Post post ->
                match post with
                | Post.SetCulture culture -> deps |> Command.setCulture culture |> deps.sendResult
                | Post.SetCultureCallback(callback, culture) ->
                    deps
                    |> Command.setCultureCallback culture
                    |> ResultAsync.bindAsync (fun _ ->
                        EA.Telegram.Endpoints.Request.Request.parse callback
                        |> ResultAsync.wrap (fun route -> consumerDeps |> entrypoint route)))

let wrap (request: EA.Telegram.Endpoints.Request.Request) callback =
    fun (consumerDeps: Consumer.Dependencies) ->
        Culture.Dependencies.create consumerDeps
        |> ResultAsync.wrap (fun deps ->
            deps.tryGetChat ()
            |> ResultAsync.bindAsync (function
                | Some chat -> consumerDeps |> callback chat
                | None -> deps |> Query.getCulturesCallback request.Value |> deps.sendResult))


let private translateButtonsGroup culture (group: Payload<ButtonsGroup>) =
    let items =
        { Id = group.Value.Name
          Value = group.Value.Name }
        :: (group.Value.Buttons
            |> Set.map (fun button ->
                { Id = button.Callback.Value
                  Value = button.Name })
            |> Set.toList)

    let request: Translation.Request = { Culture = culture; Items = items }

    request
    |> Translator.translate
    |> ResultAsync.map (fun response ->
        let responseItemsMap =
            response.Items |> List.map (fun item -> item.Id, item.Value) |> Map.ofList

        let buttonsGroupName =
            responseItemsMap
            |> Map.tryFind group.Value.Name
            |> Option.defaultValue group.Value.Name

        let buttons =
            group.Value.Buttons
            |> Set.map (fun button ->
                let buttonName =
                    responseItemsMap
                    |> Map.tryFind button.Callback.Value
                    |> Option.defaultValue button.Name

                { button with Name = buttonName })

        { group with
            Value =
                { group.Value with
                    Name = buttonsGroupName
                    Buttons = buttons } }
        |> ButtonsGroup)

let private translateText culture (text: Payload<string>) =
    let request: Translation.Request =
        { Culture = culture
          Items = [ { Id = text.Value; Value = text.Value } ] }

    request
    |> Translator.translate
    |> ResultAsync.map (fun response ->
        let responseItem =
            response.Items
            |> List.tryFind (fun item -> item.Id = text.Value)
            |> Option.defaultValue { Id = text.Value; Value = text.Value }

        { text with Value = responseItem.Value } |> Text)

let apply (culture: Culture) (msgRes: Async<Result<Message, Error'>>) =
    fun (deps: Consumer.Dependencies) ->
        msgRes
        |> ResultAsync.bindAsync (function
            | Text payload -> payload |> translateText culture
            | ButtonsGroup payload -> payload |> translateButtonsGroup culture)
