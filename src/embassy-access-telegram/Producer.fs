module EA.Telegram.Producer

open System
open Infrastructure
open Web.Telegram
open Web.Telegram.Domain
open EA.Domain
open EA.Telegram.Domain
open EA.Telegram.Persistence
open EA.Telegram.Responses

let private Admin =
    { Id = 379444553L |> ChatId
      Subscriptions = Set.empty }

let private getSubscriptionChats requestId cfg ct =
    Storage.Chat.create cfg
    |> ResultAsync.wrap (Repository.Query.Chat.getManyBySubscription requestId ct)

let private getEmbassyChats embassy cfg ct =
    Storage.Request.create cfg
    |> ResultAsync.wrap (fun storage ->
        storage
        |> Repository.Query.Chat.getEmbassyRequests embassy ct
        |> ResultAsync.map (Seq.map _.Id)
        |> ResultAsync.bindAsync (fun subIds -> storage |> Repository.Query.Chat.getManyBySubscriptions subIds ct))

let private send ct message =
    Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Client.create
    |> ResultAsync.wrap (message |> Client.Producer.produce ct)

module Produce =

    let private spread chats ct createMsg =
        chats @ [ Admin ]
        |> Seq.map (fun chat -> createMsg chat.Id |> send ct)
        |> Async.Parallel
        |> Async.map Result.choose
        |> ResultAsync.map ignore

    let notification cfg ct =
        function
        | Appointments(embassy, appointments) ->
            getEmbassyChats embassy cfg ct
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, appointments) |> Buttons.Create.appointments |> spread chats ct)
        | Confirmations(requestId, embassy, confirmations) ->
            getSubscriptionChats requestId cfg ct
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, confirmations) |> Text.Create.confirmation |> spread chats ct)
        | Fail(requestId, error) ->
            getSubscriptionChats requestId cfg ct
            |> ResultAsync.bindAsync (fun chats -> error |> Text.Create.error |> spread chats ct)
