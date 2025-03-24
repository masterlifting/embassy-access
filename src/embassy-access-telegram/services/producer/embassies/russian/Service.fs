module EA.Telegram.Services.Producer.Embassies.Russian.Service

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Services.Producer
open EA.Telegram.Dependencies.Producer.Embassies.Russian

module Kdmid =
    open EA.Telegram.Services.Embassies.Russian.Service.Kdmid

    let sendNotification notification =
        fun (deps: Kdmid.Dependencies) ->

            let translate (culture, messages) =
                deps.Culture |> Culture.Command.translateSeq culture messages

            let spreadMessages data =
                data
                |> ResultAsync.map (Seq.groupBy fst)
                |> ResultAsync.map (Seq.map (fun (culture, group) -> (culture, group |> Seq.map snd |> List.ofSeq)))
                |> ResultAsync.bindAsync (Seq.map translate >> Async.Parallel >> Async.map Result.choose)
                |> ResultAsync.map (Seq.collect id)
                |> ResultAsync.bindAsync deps.sendNotifications

            match notification with
            | Empty _ -> () |> Ok |> async.Return
            | Unsuccessfully(request, error) ->
                request
                |> deps.getRequestChats
                |> ResultAsync.map (
                    Seq.map (fun chat ->
                        chat.Culture, chat.Id |> Notification.toUnsuccessfullyResponse (request, error))
                )
                |> spreadMessages
            | HasAppointments(request, appointments) ->
                request
                |> deps.getRequestChats
                |> ResultAsync.bind (
                    Seq.map (fun chat ->
                        chat.Id
                        |> Notification.toHasAppointmentsResponse (request, appointments)
                        |> Result.map (fun x -> chat.Culture, x))
                    >> Result.choose
                )
                |> spreadMessages
            | HasConfirmations(request, confirmations) ->
                request
                |> deps.getRequestChats
                |> ResultAsync.bind (
                    Seq.map (fun chat ->
                        chat.Id
                        |> Notification.toHasConfirmationsResponse (request, confirmations)
                        |> Result.map (fun x -> chat.Culture, x))
                    >> Result.choose
                )
                |> spreadMessages
