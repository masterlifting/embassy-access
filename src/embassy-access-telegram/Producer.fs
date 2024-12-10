module EA.Telegram.Producer

open System
open Infrastructure
open Web.Telegram
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Persistence

let private send ct message =
    Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Client.init
    |> ResultAsync.wrap (fun client -> message |> Web.Telegram.Producer.produce client ct)
    |> ResultAsync.map ignore

let private spread chats ct createMsg =
    chats
    |> Seq.map (fun chat -> createMsg chat.Id |> send ct)
    |> Async.Parallel
    |> Async.map Result.choose
    |> ResultAsync.map ignore

module Produce =
    let private getSubscriptionChats requestId configuration ct =
        Storage.FileSystem.Chat.init configuration
        |> ResultAsync.wrap (fun storage ->
            requestId
            |> Query.Chat.BySubscription
            |> Repository.Query.Chat.findMany storage ct)

    let private getEmbassyChats (embassy: EmbassyGraph) configuration ct =
        configuration
        |> EA.Core.Persistence.Storage.FileSystem.Request.init
        |> ResultAsync.wrap (fun storage ->
            embassy.Name
            |> EA.Core.Persistence.Query.Request.ByEmbassyName
            |> EA.Core.Persistence.Repository.Query.Request.findMany storage ct
            |> ResultAsync.map (Seq.map _.Id)
            |> ResultAsync.bindAsync (fun subscriptions ->
                configuration
                |> Storage.FileSystem.Chat.init
                |> ResultAsync.wrap (fun storage ->
                    subscriptions
                    |> Query.Chat.BySubscriptions
                    |> Repository.Query.Chat.findMany storage ct)))

    let notification cfg ct =
        function
        | Appointments(embassy, appointments) ->
            getEmbassyChats embassy cfg ct
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, appointments)
                |> EA.Telegram.CommandHandler.Core.appointments
                |> spread chats ct)
        | Confirmations(requestId, embassy, confirmations) ->
            getSubscriptionChats requestId cfg ct
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, confirmations)
                |> EA.Telegram.CommandHandler.Core.confirmation
                |> spread chats ct)
        | Fail(requestId, error) ->
            getSubscriptionChats requestId cfg ct
            |> ResultAsync.bindAsync (fun chats -> error |> Web.Telegram.Producer.Text.createError |> spread chats ct)
