[<RequireQualifiedAccess>]
module EA.Telegram.Services.Culture.Payloads.Text

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Domain
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer

let translate culture (payload: Payload<string>) =
    fun (deps: Culture.Dependencies) ->
        let text = payload.Value
        let id = text |> hash

        let request =
            { Culture = culture
              Items = [ { Value = text } ] }

        deps.translate request
        |> ResultAsync.map (fun response ->
            response.Items
            |> List.map (fun item -> item.Value |> hash, item.Result |> Option.defaultValue item.Value)
            |> Map.ofList
            |> Map.tryFind id
            |> Option.defaultValue text)
        |> ResultAsync.map (fun value -> { payload with Value = value } |> Text)
