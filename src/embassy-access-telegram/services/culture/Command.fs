module EA.Telegram.Services.Culture.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Dependencies

let setCulture (culture: Culture) =
    fun (deps: Request.Dependencies) ->
        culture
        |> deps.setCurrentCulture
        |> ResultAsync.map (fun _ ->
            (deps.ChatId, Replace deps.MessageId)
            |> Text.create $"The language has been changed to {culture.Name}")

let setCultureCallback (culture: Culture) =
    fun (deps: Request.Dependencies) -> culture |> deps.setCurrentCulture
