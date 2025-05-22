module EA.Telegram.Services.Services.Russian.Kdmid.Command

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services.Russian
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services
open EA.Russian.Services.Domain.Kdmid

let private resultAsync = ResultAsyncBuilder()

let private createMessage chatId (request: Request<Payload>) =
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
                route.Value, a.Value)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Appointments available. Please select one."
                    Columns = 1
                    Buttons =
                        buttons
                        |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                        |> Set.ofSeq
                }
            |> Message.createNew chatId
    |> Ok
    |> async.Return

let private Limits =
    Limit.create (20u<attempts>, TimeSpan.FromDays 1) |> Set.singleton

let checkSlotsNow (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
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
                    Modified = DateTime.UtcNow
                    Payload = {
                        State = NoAppointments
                        Credentials = payloadCredentials
                        Confirmation = Disabled
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
                return processedRequest |> createMessage deps.ChatId
        }

let slotsAutoNotification (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
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
                $"Auto notification for slots enabled for service '%s{serviceId.ValueStr}' at embassy '%s{embassyId.ValueStr}' with link '%s{link}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let bookFirstSlot (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
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
                $"The booking first available slot enabled for service '%s{serviceId.ValueStr}' at embassy '%s{embassyId.ValueStr}' with link '%s{link}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let bookLastSlot (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
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
                $"The booking last available slot enabled for service '%s{serviceId.ValueStr}' at embassy '%s{embassyId.ValueStr}' with link '%s{link}'"
                |> Text.create
                |> Message.createNew deps.ChatId
                |> Ok
                |> async.Return
        }

let bookFirstSlotInPeriod
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
                $"The booking first available slot in period enabled for service '%s{serviceId.ValueStr}' at embassy '%s{embassyId.ValueStr}' with link '%s{link}'"
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

            return processedRequest |> createMessage deps.ChatId
        }

let deleteRequest requestId =
    fun (deps: Kdmid.Dependencies) ->
        deps.initRequestStorage ()
        |> ResultAsync.wrap (deps.deleteRequest requestId)
        |> ResultAsync.map (fun _ ->
            $"Request with id '%s{requestId.ValueStr}' deleted successfully."
            |> Text.create
            |> Message.tryReplace (Some deps.MessageId) deps.ChatId)

let handleProcessResult (request: Request<Payload>) =
    fun (deps: Kdmid.ProcessResult.Dependencies) ->
        
        match request.ProcessState with
        | InProcess -> Ok() |> async.Return
        | Ready -> Ok() |> async.Return
        | Failed error ->
            let createMessage chatId =
                request.Payload
                |> Payload.printError error
                |> Option.map (Text.create >> Message.createNew chatId)

            deps.getRequestChats request
            |> ResultAsync.map (
                Seq.choose (fun chat -> createMessage chat.Id |> Option.map (fun message -> chat.Culture, message))
            )
            |> ResultAsync.bindAsync deps.spreadTranslatedMessages
        | Completed _ ->
            match request.Payload.State with
            | NoAppointments -> Ok() |> async.Return
            | HasConfirmation(msg, _) ->
                let createMessage chatId =
                    msg |> Text.create |> Message.createNew chatId

                deps.getRequestChats request
                |> ResultAsync.map (Seq.map (fun chat -> createMessage chat.Id |> fun message -> chat.Culture, message))
                |> ResultAsync.bindAsync deps.spreadTranslatedMessages
            | HasAppointments appointments ->
                let createMessage chatId =
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
                        route.Value, a.Value)
                    |> fun buttons ->
                        ButtonsGroup.create {
                            Name = "Appointments available. Please select one."
                            Columns = 1
                            Buttons =
                                buttons
                                |> Seq.map (fun (callback, name) -> Button.create name (CallbackData callback))
                                |> Set.ofSeq
                        }
                    |> Message.createNew chatId

                appointments
                |> deps.setRequestsAppointments request.Embassy.Id request.Service.Id
                |> ResultAsync.bindAsync (fun _ -> deps.getRequestChats request)
                |> ResultAsync.map (Seq.map (fun chat -> createMessage chat.Id |> fun message -> chat.Culture, message))
                |> ResultAsync.bindAsync deps.spreadTranslatedMessages
