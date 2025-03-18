[<RequireQualifiedAccess>]
module EA.Telegram.Services.Culture.Payloads.Error

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open EA.Telegram.Dependencies.Consumer

let translate culture (error: Error') =
    fun (deps: Culture.Dependencies) ->
        let text = error.Message
        let id = text |> String.toHash

        let request =
            { Culture = culture
              Items = [ { Id = id; Value = text } ] }

        deps.translate request
        |> ResultAsync.map (fun response ->
            response.Items
            |> List.map (fun item -> item.Id, item.Value)
            |> Map.ofList
            |> Map.tryFind id
            |> Option.defaultValue text)
        |> ResultAsync.map error.replaceMsg