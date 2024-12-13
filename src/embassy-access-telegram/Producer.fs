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
    |> ResultAsync.wrap (fun client -> client |> Web.Telegram.Producer.produce message ct)
    |> ResultAsync.map ignore

let private spread ct =
    ResultAsync.bindAsync (fun messages ->
        messages
        |> Seq.map (send ct)
        |> Async.Parallel
        |> Async.map Result.choose
        |> ResultAsync.map ignore)

module Produce =
    open EA.Telegram.Handlers.Producer

    let notification dependencies ct =
        function
        | Appointments(embassy, appointments) ->
            dependencies |> Core.sendAppointments (embassy, appointments) |> spread ct
        | Confirmations(requestId, embassy, confirmations) ->
            dependencies
            |> Core.sendConfirmation (requestId, embassy, confirmations)
            |> spread ct
        | Fail(requestId, error) -> dependencies |> Core.sendError (requestId, error) |> spread ct
