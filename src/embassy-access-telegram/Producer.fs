module EA.Telegram.Producer

open System
open Infrastructure
open Web.Telegram
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Persistence

let private send ct message =
    Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Client.create
    |> ResultAsync.wrap (fun client -> message |> Web.Telegram.Producer.produce client ct)
    |> ResultAsync.map ignore

let private spread chats ct createMsg =
    chats
    |> Seq.map (fun chat -> createMsg chat.Id |> send ct)
    |> Async.Parallel
    |> Async.map Result.choose
    |> ResultAsync.map ignore

module Produce =
    let private getSubscriptionChats requestId cfg ct =
        Storage.FileSystem.Chat.create cfg
        |> ResultAsync.wrap (Repository.Query.Chat.getManyBySubscription requestId ct)

    let private getEmbassyChats embassy cfg ct =
        EA.Persistence.Storage.FileSystem.Request.create cfg
        |> ResultAsync.wrap (fun storage ->
            storage
            |> Repository.Query.Chat.getEmbassyRequests embassy ct
            |> ResultAsync.map (Seq.map _.Id)
            |> ResultAsync.bindAsync (fun subscriptions ->
                storage |> Repository.Query.Chat.getManyBySubscriptions subscriptions ct))

    let notification cfg ct =
        function
        | Appointments(embassy, appointments) ->
            getEmbassyChats embassy cfg ct
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, appointments) |> CommandHandler.appointments |> spread chats ct)
        | Confirmations(requestId, embassy, confirmations) ->
            getSubscriptionChats requestId cfg ct
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, confirmations) |> CommandHandler.confirmation |> spread chats ct)
        | Fail(requestId, error) ->
            getSubscriptionChats requestId cfg ct
            |> ResultAsync.bindAsync (fun chats -> error |> Web.Telegram.Producer.Text.createError |> spread chats ct)
