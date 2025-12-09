module EA.Telegram.Services.Services.Russian.Kdmid.Command

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Logging
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services
open EA.Russian.Services.Domain.Kdmid

let private resultAsync = ResultAsyncBuilder()

let handleProcessResult (request: Request<Payload>) =
    fun (deps: Kdmid.ProcessResult.Dependencies) ->

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

        let inline spreadConfirmation confirmation =
            let inline createMessage chatId =
                confirmation |> Text.create |> Message.createNew chatId

            let embassyId = request.Embassy.Id |> EmbassyId
            let serviceId = request.Service.Id |> ServiceId

            deps.getRequests embassyId serviceId
            |> ResultAsync.bindAsync (Seq.map _.Id >> deps.getChats)
            |> ResultAsync.map (Seq.map (fun chat -> createMessage chat.Id |> fun message -> chat.Culture, message))
            |> ResultAsync.bindAsync deps.spreadTranslatedMessages

        let inline spreadAppointments (appointments: Set<Appointment>) (requests: Request<Payload> seq) =
            let inline createMessage chatId =
                appointments
                |> Seq.map (fun a ->
                    let route =
                        Router.Services(
                            Services.Method.Russian(
                                Services.Russian.Method.Kdmid(
                                    Services.Russian.Kdmid.Method.Post(
                                        Services.Russian.Kdmid.Post.ConfirmAppointment(request.Id, a.Id)
                                    )
                                )
                            )
                        )
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
            | NoAppointments -> Ok() |> async.Return
            | HasConfirmation(msg, _) -> spreadConfirmation msg
            | HasAppointments appointments ->
                let embassyId = request.Embassy.Id |> EmbassyId
                let serviceId = request.Service.Id |> ServiceId
                deps.getRequests embassyId serviceId
                |> ResultAsync.map (List.sortBy _.Created)
                |> ResultAsync.map (
                    List.partition (fun r ->
                        match r.Payload.Confirmation with
                        | Disabled
                        | ForAppointment _ -> true
                        | FirstAvailable
                        | FirstAvailableInPeriod _
                        | LastAvailable -> false)
                )
                |> ResultAsync.map (fun (requestsToSpread, requestsToProcess) -> [

                    requestsToSpread
                    |> spreadAppointments appointments
                    |> ResultAsync.mapError (fun error -> deps.TaskName + error.Message |> Log.crt)

                    requestsToProcess
                    |> Seq.ofList
                    |> deps.processAllRequests
                    |> ResultAsync.mapError (fun error -> deps.TaskName + error.Message |> Log.crt)
                ])
                |> ResultAsync.mapAsync (List.iter (Async.Ignore >> Async.Start))

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
        | NoAppointments ->
            "No appointments are available at the moment."
            |> Text.create
            |> Message.createNew chatId
        | HasConfirmation(msg, _) -> msg |> Text.create |> Message.createNew chatId
        | HasAppointments appointments ->
            appointments
            |> Seq.map (fun a ->
                let route =
                    Router.Services(
                        Services.Method.Russian(
                            Services.Russian.Method.Kdmid(
                                Services.Russian.Kdmid.Method.Post(
                                    Services.Russian.Kdmid.Post.ConfirmAppointment(request.Id, a.Id)
                                )
                            )
                        )
                    )
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

let setManualRequest (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
    fun (deps: Kdmid.Dependencies) ->
        resultAsync {
            let! payloadCredentials = Credentials.parse link |> async.Return
            let! service = deps.findService serviceId
            let! embassy = deps.findEmbassy embassyId
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.tryFindRequest embassyId payloadCredentials

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
                        Confirmation = Disabled
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Manual request for service '%s{serviceId.Value}' at embassy '%s{embassyId.Value}' with link '%s{link}' has been saved and can be started from your services list."
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let startManualRequest (requestId: RequestId) =
    fun (deps: Kdmid.Dependencies) ->
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

let setAutoNotifications (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
    fun (deps: Kdmid.Dependencies) ->
        resultAsync {
            let! payloadCredentials = Credentials.parse link |> async.Return
            let! service = deps.findService serviceId
            let! embassy = deps.findEmbassy embassyId
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.tryFindRequest embassyId payloadCredentials

            let request =
                request
                |> Option.map (fun x -> {
                    x with
                        Service = service
                        ProcessState = Ready
                        Payload = {
                            x.Payload with
                                State = NoAppointments
                                Confirmation = Disabled
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
                        Confirmation = Disabled
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Automatic notifications for available slots have been enabled for service '%s{serviceId.Value}' at embassy '%s{embassyId.Value}' with link '%s{link}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let setAutoBookingFirst (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
    fun (deps: Kdmid.Dependencies) ->
        resultAsync {
            let! payloadCredentials = Credentials.parse link |> async.Return
            let! service = deps.findService serviceId
            let! embassy = deps.findEmbassy embassyId
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.tryFindRequest embassyId payloadCredentials

            let request =
                request
                |> Option.map (fun x -> {
                    x with
                        Service = service
                        ProcessState = Ready
                        Payload = {
                            x.Payload with
                                State = NoAppointments
                                Confirmation = FirstAvailable
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
                        Confirmation = FirstAvailable
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Automatic booking of the first available slot has been enabled for service '%s{serviceId.Value}' at embassy '%s{embassyId.Value}' with link '%s{link}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let setAutoBookingFirstInPeriod
    (serviceId: ServiceId)
    (embassyId: EmbassyId)
    (start: DateTime)
    (finish: DateTime)
    (link: string)
    =
    fun (deps: Kdmid.Dependencies) ->
        resultAsync {
            let! payloadCredentials = Credentials.parse link |> async.Return
            let! service = deps.findService serviceId
            let! embassy = deps.findEmbassy embassyId
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.tryFindRequest embassyId payloadCredentials

            let request =
                request
                |> Option.map (fun x -> {
                    x with
                        Service = service
                        ProcessState = Ready
                        Payload = {
                            x.Payload with
                                State = NoAppointments
                                Confirmation = FirstAvailableInPeriod(start, finish)
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
                        Confirmation = FirstAvailableInPeriod(start, finish)
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Automatic booking of the first available slot within the specified period has been enabled for service '%s{serviceId.Value}' at embassy '%s{embassyId.Value}' with link '%s{link}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let setAutoBookingLast (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
    fun (deps: Kdmid.Dependencies) ->
        resultAsync {
            let! payloadCredentials = Credentials.parse link |> async.Return
            let! service = deps.findService serviceId
            let! embassy = deps.findEmbassy embassyId
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.tryFindRequest embassyId payloadCredentials

            let request =
                request
                |> Option.map (fun x -> {
                    x with
                        Service = service
                        ProcessState = Ready
                        Payload = {
                            x.Payload with
                                State = NoAppointments
                                Confirmation = LastAvailable
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
                        Confirmation = LastAvailable
                    }
                }

            do! requestStorage |> deps.createOrUpdateRequest request
            do! deps.tryAddSubscription request

            return
                $"Automatic booking of the last available slot has been enabled for service '%s{serviceId.Value}' at embassy '%s{embassyId.Value}' with link '%s{link}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let confirmAppointment (requestId: RequestId) (appointmentId: AppointmentId) =
    fun (deps: Kdmid.Dependencies) ->
        resultAsync {
            let! requestStorage = deps.initRequestStorage () |> async.Return
            let! request = requestStorage |> deps.findRequest requestId
            let! processedRequest =
                requestStorage
                |> deps.processRequest {
                    request with
                        Request.Payload.Confirmation = ForAppointment appointmentId
                }

            return processedRequest |> handleRequestResult deps.ChatId
        }

let deleteRequest requestId =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.deleteRequest requestId)
        |> ResultAsync.map (fun _ ->
            $"Subscription with ID '%s{requestId.Value}' has been deleted successfully."
            |> Text.create
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)
