module EA.Telegram.Services.Producer.Embassies.Russian.Service

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Dependencies.Producer.Embassies.RussianEmbassy

module Kdmid =
    open EA.Telegram.Services.Embassies.Russian.Service.Kdmid

    let sendNotification notification =
        fun (deps: Kdmid.Dependencies) ->
            match notification with
            | Successfully(request, msg) ->
                request
                |> deps.getRequestChats
                |> ResultAsync.map (Seq.map (fun chat -> chat.Id |> Notification.toSuccessfullyResponse (request, msg)))
                |> ResultAsync.bindAsync deps.sendNotifications
            | Unsuccessfully(request, error) ->
                request
                |> deps.getRequestChats
                |> ResultAsync.map (
                    Seq.map (fun chat -> chat.Id |> Notification.toUnsuccessfullyResponse (request, error))
                )
                |> ResultAsync.bindAsync deps.sendNotifications
            | HasAppointments(request, appointments) ->
                request
                |> deps.getRequestChats
                |> ResultAsync.bind (
                    Seq.map (fun chat -> chat.Id |> Notification.toHasAppointmentsResponse (request, appointments))
                    >> Result.choose
                )
                |> ResultAsync.bindAsync (Seq.ofList >> deps.sendNotifications)
            | HasConfirmations(request, confirmations) ->
                request
                |> deps.getRequestChats
                |> ResultAsync.bind (
                    Seq.map (fun chat -> chat.Id |> Notification.toHasConfirmationsResponse (request, confirmations))
                    >> Result.choose
                )
                |> ResultAsync.bindAsync (Seq.ofList >> deps.sendNotifications)
