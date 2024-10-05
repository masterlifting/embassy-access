module EmbassyAccess.Telegram.Producer

open System
open EmbassyAccess.Domain
open Infrastructure
open Persistence.Domain
open Web.Telegram
open Web.Telegram.Domain

let private AdminChatId = 379444553L

let private getChat ct requestId =
    Persistence.Storage.create InMemory
    |> ResultAsync.wrap (fun storage ->
        let filter = Persistence.Filter.Chat.Search requestId
        storage |> Persistence.Repository.Query.Chat.get ct filter)

let private send ct message =
    Domain.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Client.create
    |> ResultAsync.wrap (message |> Client.Producer.produce ct)

module Produce =
    let notification ct =
        function
        | Appointments(requestId, embassy, appointments) ->
            requestId
            |> getChat ct
            |> ResultAsync.bind (fun chat ->
                chat
                |> Option.map (fun chat ->
                    (embassy, appointments)
                    |> Message.Create.Buttons.appointments chat.Id
                    |> send ct)
                |> Option.defaultValue ($"Chat for {requestId}." |> NotFound |> Error |> async.Return))
        | Confirmations(requestId, embassy, confirmations) ->
            requestId
            |> getChat ct
            |> ResultAsync.bind (fun chat ->
                chat
                |> Option.map (fun chat ->
                    (embassy, confirmations) |> Message.Create.Text.confirmation chat.Id |> send ct)
                |> Option.defaultValue ($"Chat for {requestId}." |> NotFound |> Error |> async.Return))
        | Fail(requestId, error) -> error |> Message.Create.Text.error AdminChatId |> send ct
