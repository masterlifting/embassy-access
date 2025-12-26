module EA.Telegram.Features.Embassies.Italian.Prenotami.Command

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Italian.Services
open EA.Italian.Services.Domain.Prenotami
open EA.Telegram.Router.Embassies
open EA.Telegram.Router.Embassies.Italian
open EA.Telegram.Router.Embassies.Italian.Prenotami
open EA.Telegram.Features.Dependencies.Embassies.Italian

let private buildRoute route =
    EA.Telegram.Router.Route.Embassies(Italian(Prenotami route))

let private resultAsync = ResultAsyncBuilder()

let handleProcessResult (request: Request<Payload>) =
    fun (deps: Prenotami.ProcessResult.Dependencies) ->

        let inline spreadFailure error =
            let inline createMessage chatId =
                request.Payload
                |> Payload.printError error
                |> Option.map (Text.create >> Message.createNew chatId)

            let embassyId = request.Embassy.Id |> EmbassyId
            let serviceId = request.Service.Id |> ServiceId

            deps.getRequests embassyId serviceId
            |> ResultAsync.bindAsync (Seq.map _.Id >> deps.getChats)
            |> ResultAsync.map (
                Seq.choose (fun chat -> createMessage chat.Id |> Option.map (fun message -> chat.Culture, message))
            )
            |> ResultAsync.bindAsync deps.spreadTranslatedMessages

        let inline spreadAppointments (appointments: Set<Appointment>) (requests: Request<Payload> seq) =
            let inline createMessage chatId =
                appointments
                |> Seq.map (fun a ->
                    let route = Post(ConfirmAppointment(request.Id, a.Id)) |> buildRoute
                    a |> Appointment.print, route.Value)
                |> fun buttons ->
                    let serviceName = request.Service.Value.BuildName 1 "."
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
            | NoAppointments _ -> Ok() |> async.Return
            | HasAppointments appointments ->

                let embassyId = request.Embassy.Id |> EmbassyId
                let serviceId = request.Service.Id |> ServiceId

                deps.getRequests embassyId serviceId
                |> ResultAsync.bindAsync (spreadAppointments appointments)

let private handleRequestResult chatId (request: Request<Payload>) =
    match request.ProcessState with
    | InProcess ->
        "Your request is still being processed. Please wait for the result."
        |> Text.create
        |> Message.createNew chatId
    | Ready ->
        "Your request has not been started yet. Please start it first."
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
        | NoAppointments msg -> msg |> Text.create |> Message.createNew chatId
        | HasAppointments appointments ->
            appointments
            |> Seq.map (fun a ->
                let route = Post(ConfirmAppointment(request.Id, a.Id)) |> buildRoute
                a |> Appointment.print, route.Value)
            |> fun buttons ->
                let serviceName = request.Service.Value.BuildName 1 "."
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
                                State = NoAppointments "No appointments yet."
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
                        State = NoAppointments "No appointments yet."
                        Credentials = payloadCredentials
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Manual request for service '%s{serviceId.Value}' at embassy '%s{embassyId.Value}' with login '%s{login}' has been saved and can be started from your services list."
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
                            State = NoAppointments "No appointments yet."
                    }
            }
            match request.ProcessState with
            | InProcess ->
                return
                    "Your request is still being processed. Please wait for the result."
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
                                State = NoAppointments "No appointments yet."
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
                        State = NoAppointments "No appointments yet."
                        Credentials = payloadCredentials
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Automatic notifications for available slots have been enabled for service '%s{serviceId.Value}' at embassy '%s{embassyId.Value}' with login '%s{login}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let confirmAppointment (requestId: RequestId) (appointmentId: AppointmentId) =
    fun (deps: Prenotami.Dependencies) ->
        $"Confirmation of appointment '%s{appointmentId.ValueStr}' for request '%s{requestId.Value}' is not implemented yet. "
        + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return

let deleteRequest requestId =
    fun (deps: Prenotami.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.deleteRequest requestId)
        |> ResultAsync.map (fun _ ->
            $"Subscription with ID '%s{requestId.Value}' has been deleted successfully."
            |> Text.create
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)
