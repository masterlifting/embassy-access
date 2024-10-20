﻿[<RequireQualifiedAccess>]
module EA.Telegram.Responses.Text

open System
open Infrastructure
open Web.Telegram.Domain.Producer
open EA.Telegram.Persistence
open EA.Telegram.Responses
open EA.Telegram.Domain
open EA.Domain


module Create =

    let error (error: Error') =
        fun chatId -> error.Message |> Response.create (chatId, New) |> Text

    let confirmation (embassy, (confirmations: Set<Confirmation>)) =
        fun chatId ->
            confirmations
            |> Seq.map (fun confirmation -> $"'{embassy}'. Confirmation: {confirmation.Description}")
            |> String.concat "\n"
            |> Response.createText (chatId, New)

    let subscriptionRequest (embassy', country', city') =
        fun (chatId, msgId) ->
            match (embassy', country', city') |> EA.Mapper.Embassy.createInternal with
            | Error error -> error.Message
            | Ok embassy ->
                match embassy with
                | Russian _ ->
                    let key = [ Key.SUB; embassy'; country'; city' ] |> Key.wrap
                    $"Send your payload using the following format: '{key}|your_link_here'."
                | _ -> $"Not supported embassy: '{embassy'}'."
            |> Response.createText (chatId, msgId |> Replace)
            |> Ok
            |> async.Return

    let userSubscriptions (embassy', country', city') =
        fun (chatId, msgId) cfg ct ->
            EA.Mapper.Embassy.createInternal (embassy', country', city')
            |> ResultAsync.wrap (fun embassy ->
                Storage.Chat.create cfg
                |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
                |> ResultAsync.bindAsync (function
                    | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                    | Some chat ->
                        Storage.Request.create cfg
                        |> ResultAsync.wrap (Repository.Query.Chat.getChatRequests chat ct))
                |> ResultAsync.map (Seq.filter (fun request -> request.Embassy = embassy))
                |> ResultAsync.map (fun requests ->
                    requests
                    |> Seq.map (fun request -> $"{request.Id} -> {request.Payload}")
                    |> String.concat Environment.NewLine
                    |> (Response.createText (chatId, msgId |> Replace))))

    let subscribe (embassy', country', city', payload) =
        fun chatId cfg ct ->
            (embassy', country', city')
            |> EA.Mapper.Embassy.createInternal
            |> ResultAsync.wrap (fun embassy ->
                match embassy with
                | Russian _ ->

                    let createOrUpdatePassportSearchRequest ct =
                        Storage.Request.create cfg
                        |> ResultAsync.wrap (
                            Repository.Command.Request.createOrUpdatePassportSearch (embassy, payload) ct
                        )

                    let createOrUpdateChatSubscription ct =
                        ResultAsync.bindAsync (fun (request: Request) ->
                            Storage.Chat.create cfg
                            |> ResultAsync.wrap (
                                Repository.Command.Chat.createOrUpdateSubscription (chatId, request.Id) ct
                            )
                            |> ResultAsync.map (fun _ -> request))

                    createOrUpdatePassportSearchRequest ct
                    |> createOrUpdateChatSubscription ct
                    |> ResultAsync.map (fun request -> $"Subscription has been activated for '{request.Embassy}'.")
                    |> ResultAsync.map (Response.createText (chatId, New))
                | _ -> embassy' |> NotSupported |> Error |> async.Return)

    let private confirmRussianAppointment request ct storage =
        let config: EA.Embassies.Russian.Domain.ProcessRequestConfiguration =
            { TimeShift = 0y }

        (storage, config, ct) |> EA.Deps.Russian.processRequest |> EA.Api.processRequest
        <| request

    let confirmAppointment (embassy', country',city', payload) =
        fun chatId cfg ct ->
            (embassy', country', city')
            |> EA.Mapper.Embassy.createInternal
            |> Result.bind (fun embassy -> Storage.Request.create cfg |> Result.map (fun storage -> (embassy, storage)))
            |> ResultAsync.wrap (fun (embassy, storage) ->
                storage
                |> Repository.Query.Chat.getChatEmbassyRequests chatId embassy ct
                |> ResultAsync.bindAsync (Seq.map(fun request -> 
                    match
                        request.Appointments
                        |> Seq.tryFind (fun appointment -> appointment.Value = payload)
                    with
                    | None -> "Appointment" |> NotFound |> Error |> async.Return
                    | Some appointment ->
                        let request =
                            { request with
                                ConfirmationState = Manual appointment }

                        match request.Embassy with
                        | Russian _ -> storage |> confirmRussianAppointment request ct
                        | _ -> "Embassy" |> NotSupported |> Error |> async.Return
                        |> ResultAsync.map (fun request ->
                            let confirmation =
                                request.Appointments
                                |> Seq.tryFind (fun appointment -> appointment.Value = payload)
                                |> Option.map (fun appointment ->
                                    appointment.Confirmation
                                    |> Option.map _.Description
                                    |> Option.defaultValue "Not found")

                            $"'{request.Embassy}'. Confirmation: {confirmation}")
                        |> ResultAsync.map (Response.createText (chatId, New)))))
                |> Async.Parallel
