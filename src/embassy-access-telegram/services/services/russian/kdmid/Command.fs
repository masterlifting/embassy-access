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
        | NoAppointments ->
            "No appointments found."
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
                route.Value, a.Value)
            |> fun buttons ->
                ButtonsGroup.create {
                    Name = "Choose the appointment"
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
            let! requestStorage = deps.initKdmidRequestStorage () |> async.Return
            let! requests = requestStorage |> deps.findRequests embassyId serviceId

            let request =
                requests
                |> Seq.tryFind (fun x -> x.Payload.Credentials = payloadCredentials)
                |> Option.defaultValue {
                    Id = RequestId.createNew ()
                    Service = service
                    Embassy = embassy
                    ProcessState = Ready
                    Limits = Limits
                    UseBackground = false
                    Modified = DateTime.UtcNow
                    Payload = {
                        Credentials = payloadCredentials
                        State = NoAppointments
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
                let! processedRequest = requestStorage |> deps.processRequest request
                return processedRequest |> createMessage deps.ChatId
        }

let slotsAutoNotification (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
    fun (deps: Kdmid.Dependencies) ->
        $"Auto notification for slots enabled for service {serviceId.ValueStr} at embassy {embassyId.ValueStr} with link {link}."
        |> Text.create
        |> Message.createNew deps.ChatId
        |> Ok
        |> async.Return

let bookFirstSlot (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
    fun (deps: Kdmid.Dependencies) ->
        $"Attempting to book first available slot for service {serviceId.ValueStr} at embassy {embassyId.ValueStr} with link {link}..."
        |> Text.create
        |> Message.createNew deps.ChatId
        |> Ok
        |> async.Return

let bookLastSlot (serviceId: ServiceId) (embassyId: EmbassyId) (link: string) =
    fun (deps: Kdmid.Dependencies) ->
        $"Attempting to book last available slot for service {serviceId.ValueStr} at embassy {embassyId.ValueStr} with link {link}..."
        |> Text.create
        |> Message.createNew deps.ChatId
        |> Ok
        |> async.Return

let bookFirstSlotInPeriod
    (serviceId: ServiceId)
    (embassyId: EmbassyId)
    (start: DateTime)
    (finish: DateTime)
    (link: string)
    =
    fun (deps: Kdmid.Dependencies) ->
        $"Attempting to book first available slot in period for service {serviceId.ValueStr} at embassy {embassyId.ValueStr} with link {link}..."
        |> Text.create
        |> Message.createNew deps.ChatId
        |> Ok
        |> async.Return

let confirmAppointment (requestId: RequestId) (appointmentId: AppointmentId) =
    fun (deps: Kdmid.Dependencies) ->
        $"This operation is not supported yet. Please try again later."
        |> NotSupported
        |> Error
        |> async.Return
