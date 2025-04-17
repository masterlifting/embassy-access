module EA.Telegram.Services.Culture.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Telegram.Dependencies

let setCulture (culture: Culture) chatId messageId =
    fun (deps: Culture.Dependencies) ->
        culture
        |> deps.setCurrent
        |> ResultAsync.map (fun _ ->
            (chatId, Replace messageId)
            |> Text.create $"The language has been changed to the '{culture.Name}'")

let setCultureCallback (culture: Culture) =
    fun (deps: Culture.Dependencies) -> culture |> deps.setCurrent
