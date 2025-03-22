﻿[<RequireQualifiedAccess>]
module EA.Telegram.Services.Culture.Payloads.Text

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open Web.Telegram.Domain.Producer
open AIProvider.Services.Dependencies

let translate culture placeholder (payload: Payload<string>) =
    fun (deps: Culture.Dependencies) ->
        let text = payload.Value

        let request =
            { Culture = culture
              Placeholder = placeholder
              Items = [ { Value = text } ] }

        deps.translate request
        |> ResultAsync.map (fun response ->
            response.Items
            |> List.map (fun item -> item.Value, item.Result |> Option.defaultValue item.Value)
            |> Map.ofList
            |> Map.tryFind text
            |> Option.defaultValue text)
        |> ResultAsync.map (fun value -> { payload with Value = value } |> Text)
