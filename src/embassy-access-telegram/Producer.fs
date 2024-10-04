module EmbassyAccess.Telegram.Producer

open System
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open Infrastructure
open Web.Telegram.Domain
open EmbassyAccess
open Persistence.Domain

let getChat ct request =
    Persistence.Storage.create InMemory
    |> ResultAsync.wrap (fun storage ->
        let filter = Filter.Telegram.Chat.Search request.Id
        storage |> Repository.Query.Telegram.Chat.get ct filter)

let private send ct data =
    EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Client.create
    |> ResultAsync.wrap (data |> Web.Telegram.Client.send ct)

module Produce =
    let notification ct =
        function
        | SearchAppointments request ->
            request
            |> Core.Create.appointments
            |> Option.map (fun (embassy, appointments) ->
                (embassy, appointments)
                |> Data.Create.Buttons.appointments AdminChatId
                |> send ct)
        | MakeConfirmations request ->
            request
            |> Core.Create.confirmations
            |> Option.map (fun (requestId, embassy, confirmations) ->
                (requestId, embassy, confirmations)
                |> Data.Create.Message.confirmation AdminChatId
                |> send ct)
        | Error(requestId, error) -> error |> Data.Create.Message.error AdminChatId |> Option.map (send ct)
