﻿module EmbassyAccess.Telegram.Producer

open System
open Infrastructure
open Persistence.Domain
open Web.Telegram
open Web.Telegram.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Telegram.Domain

let private Admin =
    { Id = 379444553L |> ChatId
      Subscriptions = Set.empty }

let private getChats ct requestId =
    Persistence.Storage.create InMemory
    |> ResultAsync.wrap (fun storage ->
        let query = Persistence.Query.Chat.Search requestId
        storage |> Persistence.Repository.Query.Chat.getMany ct query)

let private send ct message =
    EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Client.create
    |> ResultAsync.wrap (message |> Client.Producer.produce ct)

module Produce =

    let private send ct requestId createMsg =
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
            (embassy, appointments)
            |> Message.Create.Buttons.appointments
            |> send ct requestId
        | Confirmations(requestId, embassy, confirmations) ->
            (embassy, confirmations)
            |> Message.Create.Text.confirmation
            |> send ct requestId
        | Fail(requestId, error) -> error |> Message.Create.Text.error |> send ct requestId
