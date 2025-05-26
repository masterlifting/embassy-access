﻿module EA.Telegram.Services.Services.Italian.Prenotami.Command

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Italian
open EA.Telegram.Dependencies.Services.Italian
open EA.Italian.Services
open EA.Italian.Services.Domain.Prenotami

let private resultAsync = ResultAsyncBuilder()

let handleProcessResult (request: Request<Payload>) =
    fun (deps: Prenotami.ProcessResult.Dependencies) ->

        let inline spreadFailure error =
            let inline createMessage chatId =
                request.Payload
                |> Payload.printError error
                |> Option.map (Text.create >> Message.createNew chatId)

            deps.getRequests request.Embassy.Id request.Service.Id
            |> ResultAsync.bindAsync (Seq.map _.Id >> deps.getChats)
            |> ResultAsync.map (
                Seq.choose (fun chat -> createMessage chat.Id |> Option.map (fun message -> chat.Culture, message))
            )
            |> ResultAsync.bindAsync deps.spreadTranslatedMessages

        let inline spreadAppointments (appointments: Set<Appointment>) (requests: Request<Payload> seq) =
            let inline createMessage chatId =
                appointments
                |> Seq.map (fun a ->
                    let route =
                        Router.Services(
                            Services.Method.Italian(
                                Services.Italian.Method.Prenotami(
                                    Services.Italian.Prenotami.Method.Post(
                                        Services.Italian.Prenotami.Post.ConfirmAppointment(request.Id, a.Id)
                                    )
                                )
                            )
                        )
                    a |> Appointment.print, route.Value)
                |> fun buttons ->
                    let serviceName = request.Service.BuildName 1 "."
                    ButtonsGroup.create {
                        Name = $"Available appointments for %s{serviceName}"
                        Columns = 1
                        Buttons = buttons |> ButtonsGroup.createButtons
                    }
                |> Message.createNew chatId

            requests
            |> Seq.map (fun r -> {
                r with
                    Payload = {
                        r.Payload with
                            State = HasAppointments appointments
                    }
            })
            |> deps.updateRequests
            |> ResultAsync.bindAsync (Seq.map _.Id >> deps.getChats)
            |> ResultAsync.map (Seq.map (fun chat -> createMessage chat.Id |> fun message -> chat.Culture, message))
            |> ResultAsync.bindAsync deps.spreadTranslatedMessages

        match request.ProcessState with
        | InProcess -> Ok() |> async.Return
        | Ready -> Ok() |> async.Return
        | Failed error -> spreadFailure error
        | Completed _ ->
            match request.Payload.State with
            | NoAppointments -> Ok() |> async.Return
            | HasAppointments appointments ->
                deps.getRequests request.Embassy.Id request.Service.Id
                |> ResultAsync.bindAsync (spreadAppointments appointments)

let private handleRequestResult chatId (request: Request<Payload>) =
    match request.ProcessState with
    | InProcess ->
        "The request is still in process. Please wait for the result."
        |> Text.create
        |> Message.createNew chatId
    | Ready ->
        "The request is is not started yet. Please start it first."
        |> Text.create
        |> Message.createNew chatId
    | Failed error ->
        request.Payload
        |> Payload.printError error
        |> Option.defaultValue "Something went wrong. Please try again later."
        |> Text.create
        |> Message.createNew chatId
    | Completed _ ->
        match request.Payload.State with
        | NoAppointments -> "No appointments found for now." |> Text.create |> Message.createNew chatId
        | HasAppointments appointments ->
            appointments
            |> Seq.map (fun a ->
                let route =
                    Router.Services(
                        Services.Method.Italian(
                            Services.Italian.Method.Prenotami(
                                Services.Italian.Prenotami.Method.Post(
                                    Services.Italian.Prenotami.Post.ConfirmAppointment(request.Id, a.Id)
                                )
                            )
                        )
                    )
                a |> Appointment.print, route.Value)
            |> fun buttons ->
                let serviceName = request.Service.BuildName 1 "."
                ButtonsGroup.create {
                    Name = $"Available appointments for %s{serviceName}"
                    Columns = 1
                    Buttons = buttons |> ButtonsGroup.createButtons
                }
            |> Message.createNew chatId
    |> Ok
    |> async.Return

let private Limits =
    Set [
        Limit.init (20u<attempts>, TimeSpan.FromDays 1)
        Limit.init (1u<attempts>, TimeSpan.FromMinutes 5.0)
    ]

let setManualRequest (serviceId: ServiceId) (embassyId: EmbassyId) (login: string) (password: string) =
    fun (deps: Prenotami.Dependencies) ->
        resultAsync {
            let! payloadCredentials = Credentials.parse login password |> async.Return
            let! service = deps.findService serviceId
            let! embassy = deps.findEmbassy embassyId
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.tryFindRequest embassyId serviceId payloadCredentials

            let request =
                request
                |> Option.map (fun x -> {
                    x with
                        Payload = {
                            x.Payload with
                                State = NoAppointments
                        }
                })
                |> Option.defaultValue {
                    Id = RequestId.createNew ()
                    Service = service
                    Embassy = embassy
                    ProcessState = Ready
                    Limits = Limits
                    Created = DateTime.UtcNow
                    Modified = DateTime.UtcNow
                    Payload = {
                        State = NoAppointments
                        Credentials = payloadCredentials
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Manual request for service '%s{serviceId.ValueStr}' at embassy '%s{embassyId.ValueStr}'  with login '%s{login}' saved and can be started from your services list."
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let startManualRequest (requestId: RequestId) =
    fun (deps: Prenotami.Dependencies) ->
        resultAsync {
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.findRequest requestId

            let request = {
                request with
                    Payload = {
                        request.Payload with
                            State = NoAppointments
                    }
            }
            match request.ProcessState with
            | InProcess ->
                return
                    "The request is still in process. Please wait for the result."
                    |> Text.create
                    |> Message.createNew deps.ChatId
                    |> Ok
                    |> async.Return
            | Ready
            | Failed _
            | Completed _ ->
                do! deps.tryAddSubscription request
                let! processedRequest = requestStorage |> deps.processRequest request
                return processedRequest |> handleRequestResult deps.ChatId
        }

let setAutoNotifications (serviceId: ServiceId) (embassyId: EmbassyId) (login: string) (password: string) =
    fun (deps: Prenotami.Dependencies) ->
        resultAsync {
            let! payloadCredentials = Credentials.parse login password |> async.Return
            let! service = deps.findService serviceId
            let! embassy = deps.findEmbassy embassyId
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.tryFindRequest embassyId serviceId payloadCredentials

            let request =
                request
                |> Option.map (fun x -> {
                    x with
                        Service = service
                        ProcessState = Ready
                        Payload = {
                            x.Payload with
                                State = NoAppointments
                        }
                })
                |> Option.defaultValue {
                    Id = RequestId.createNew ()
                    Service = service
                    Embassy = embassy
                    ProcessState = Ready
                    Limits = Limits
                    Created = DateTime.UtcNow
                    Modified = DateTime.UtcNow
                    Payload = {
                        State = NoAppointments
                        Credentials = payloadCredentials
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Auto notification for slots enabled for service '%s{serviceId.ValueStr}' at embassy '%s{embassyId.ValueStr}' with login '%s{login}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let confirmAppointment (requestId: RequestId) (appointmentId: AppointmentId) =
    fun (deps: Prenotami.Dependencies) ->
        $"The confirmation of appointments '%s{appointmentId.ValueStr}' for request '%s{requestId.ValueStr}' is not implemented yet. "
        + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let deleteRequest requestId =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.deleteRequest requestId)
        |> ResultAsync.map (fun _ ->
            $"Request with id '%s{requestId.ValueStr}' deleted successfully."
            |> Text.create
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)
