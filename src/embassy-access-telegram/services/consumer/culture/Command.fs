module EA.Telegram.Services.Consumer.Culture.Command

open System
open EA.Telegram.Domain
open Infrastructure.Prelude
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
