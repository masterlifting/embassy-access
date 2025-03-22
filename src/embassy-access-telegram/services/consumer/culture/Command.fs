module EA.Telegram.Services.Consumer.Culture.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer

let setCulture (culture: Culture) =
    fun (deps: Culture.Dependencies) ->
        culture
        |> deps.setCurrentCulture
        |> ResultAsync.map (fun _ ->
            (deps.ChatId, Replace deps.MessageId)
            |> Text.create $"The language has been changed to {culture.Name}")

let setCultureCallback (culture: Culture) =
    fun (deps: Culture.Dependencies) -> culture |> deps.setCurrentCulture

