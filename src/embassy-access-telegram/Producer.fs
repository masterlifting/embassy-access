module EA.Telegram.Producer

open System
open Infrastructure.Prelude
open Web.Telegram
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain

let private send ct message =
    Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Client.init
    |> ResultAsync.wrap (fun client -> message |> Web.Telegram.Producer.produce ct client)
    |> ResultAsync.map ignore

let private spread chats ct createMsg =
    chats
    |> Seq.map (fun chat -> createMsg chat.Id |> send ct)
    |> Async.Parallel
    |> Async.map Result.choose
    |> ResultAsync.map ignore

module Produce =
    open EA.Telegram.DataAccess
    open EA.Core.DataAccess

    let private getSubscriptionChats requestId (deps: Dependencies.Producer.Dependencies) =
        deps.initChatStorage ()
        |> ResultAsync.wrap (Chat.Query.findManyBySubscription requestId)

    let private getEmbassyChats (embassy: EmbassyGraph) (deps: Dependencies.Producer.Dependencies) =
        deps.initRequestStorage ()
        |> ResultAsync.wrap (Request.Query.findManyByEmbassyName embassy.Name)
        |> ResultAsync.map (Seq.map _.Id)
        |> ResultAsync.bindAsync (fun subscriptions ->
            deps.initChatStorage ()
            |> ResultAsync.wrap (Chat.Query.findManyBySubscriptions subscriptions))

    let notification deps ct =
        function
        | Appointments(embassy, appointments) ->
            getEmbassyChats embassy deps
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, appointments)
                |> EA.Telegram.CommandHandler.Core.appointments
                |> spread chats ct)
        | Confirmations(requestId, embassy, confirmations) ->
            getSubscriptionChats requestId deps
            |> ResultAsync.bindAsync (fun chats ->
                (embassy, confirmations)
                |> EA.Telegram.CommandHandler.Core.confirmation
                |> spread chats ct)
        | Fail(requestId, error) ->
            getSubscriptionChats requestId deps
            |> ResultAsync.bindAsync (fun chats -> error |> Web.Telegram.Producer.Text.createError |> spread chats ct)
