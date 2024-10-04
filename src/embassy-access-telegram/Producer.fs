module EmbassyAccess.Telegram.Producer

open System
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open Infrastructure
open Web.Telegram.Domain
open EmbassyAccess
open Persistence.Domain

let private AdminChatId = 379444553L

let private getChat ct requestId =
    Persistence.Storage.create InMemory
    |> ResultAsync.wrap (fun storage ->
        let filter = Filter.Telegram.Chat.Search requestId
        storage |> Repository.Query.Telegram.Chat.get ct filter)

let private send ct message =
    Domain.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> ResultAsync.wrap (message |> Web.Telegram.Client.send ct)

module Produce =
    let notification ct =
        function
        | Appointments(requestId, embassy, appointments) ->
            (embassy, appointments)
            |> Message.Create.Buttons.appointments AdminChatId
            |> send ct
        | Confirmations(requestId, embassy, confirmations) ->
            (embassy, confirmations)
            |> Message.Create.Text.confirmation AdminChatId
            |> send ct
        | Error(requestId, error) -> error |> Message.Create.Text.error AdminChatId |> send ct
