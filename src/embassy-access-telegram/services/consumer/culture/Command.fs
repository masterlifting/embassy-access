module EA.Telegram.Services.Consumer.Culture.Command

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer

let setCulture (code: string) =
    fun (deps: Culture.Dependencies) ->
        code
        |> deps.setCurrentCulture
        |> ResultAsync.map (fun _ -> (deps.ChatId, Replace deps.MessageId) |> Text.create $"Culture set to {code}")
