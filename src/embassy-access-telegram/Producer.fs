module EA.Telegram.Producer

open System
open Infrastructure
open Persistence.Domain
open Web.Telegram
open Web.Telegram.Domain
open EA.Domain
open EA.Telegram.Domain
open EA.Telegram.Persistence
open EA.Telegram.Responses

let private Admin =
    { Id = 379444553L |> ChatId
      Subscriptions = Set.empty }

let private getChats ct requestId =
    Persistence.Storage.create Storage.Context.InMemory
    |> ResultAsync.wrap (fun storage ->
        let query = Query.Chat.SearchSubscription requestId
        storage |> Repository.Query.Chat.getMany ct query)

let private send ct message =
    Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Client.create
    |> ResultAsync.wrap (message |> Client.Producer.produce ct)

module Produce =

    let private send requestId ct createMsg =
        requestId
        |> getChats ct
        |> ResultAsync.bindAsync (fun chats ->
            chats @ [ Admin ]
            |> Seq.map (fun chat -> createMsg chat.Id |> send ct)
            |> Async.Parallel
            |> Async.map Result.choose)
        |> ResultAsync.map ignore

    let notification ct =
        function
        | Appointments(requestId, embassy, appointments) ->
            (embassy, appointments) |> Buttons.Create.appointments |> send requestId ct
        | Confirmations(requestId, embassy, confirmations) ->
            (embassy, confirmations) |> Text.Create.confirmation |> send requestId ct
        | Fail(requestId, error) -> error |> Text.Create.error |> send requestId ct
