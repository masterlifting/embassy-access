module EA.Telegram.Services.Embassies.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Telegram.Producer
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Telegram.Router.Embassies
open EA.Telegram.Services.Embassies
open EA.Telegram.Dependencies.Embassies

let subscribe (model: Post.Model.Subscribe) =
    fun (deps: Embassies.Dependencies) ->
        let resultAsync = ResultAsyncBuilder()

        resultAsync {
            let! requestOpt =
                deps.getChatRequests ()
                |> ResultAsync.map (
                    List.tryFind (fun request ->
                        request.Service.Id = model.ServiceId
                        && request.Service.Embassy.Id = model.EmbassyId
                        && request.Service.Payload = model.Payload)
                )

            let! message =
                match requestOpt with
                | Some request ->
                    $"Subscription for the service '{request.Service.Name}' for the embassy '{request.Service.Embassy.Name}' already exists."
                    |> Ok
                    |> async.Return
                | None ->
                    resultAsync {
                        let! service = deps.getService model.ServiceId
                        let! embassy = deps.getEmbassy model.EmbassyId
                        let! request =
                            deps.createRequest (
                                model.Payload,
                                service,
                                embassy,
                                model.IsBackground,
                                model.ConfirmationState
                            )

                        return
                            $"Subscription '{request.Id.ValueStr}' for the service '{service.Name}' for the embassy '{embassy.Name}' has been created."
                            |> Ok
                            |> async.Return
                    }

            return (deps.Chat.Id, New) |> Text.create message |> Ok |> async.Return
        }

let checkAppointments (model: Post.Model.CheckAppointments) =
    fun (deps: Embassies.Dependencies) ->
        let resultAsync = ResultAsyncBuilder()

        resultAsync {

            let! requestOpt =
                deps.getChatRequests ()
                |> ResultAsync.map (
                    List.tryFind (fun request ->
                        request.Service.Id = model.ServiceId
                        && request.Service.Embassy.Id = model.EmbassyId
                        && request.Service.Payload = model.Payload)
                )
            
            let printPayload, processRequest =
                match model.EmbassyId.Split() |> Seq.skip 1 |> Seq.tryHead with
                | Some Embassies.RUS ->
                    match model.ServiceId.Split() with
                    | [ _; Embassies.RUS; "0"; "0"; "1" ] ->
                        let deps: EA.Russian.Clients.Domain.Midpass.Dependencies =
                            {
                                Number = model.Payload
                            }
                        let processRequest =
                            deps |> EA.Russian.Clients.Midpass.Service.tryProcess
                        let printPayload =
                            deps |> EA.Russian.Clients.Domain.Midpass.Payload.create >> Result.map EA.Russian.Clients.Domain.Midpass.Payload.print
                        (printPayload, processRequest) |> Ok
                    | _ ->
                        deps.Russian
                        |> EA.Telegram.Dependencies.Embassies.Russian.Kdmid.Dependencies.create
                        |> Result.map (fun x -> (x.printPayload, x.processRequest))
                | _ -> $"{model.EmbassyId.Value} is not implemented yet. " + NOT_IMPLEMENTED |> NotImplemented |> Error
                

            return
                match requestOpt with
                | Some request ->
                    match request.ProcessState with
                    | InProcess ->
                        (deps.Chat.Id, New)
                        |> Text.create
                            $"The request for the service '{request.Service.Name}' for the embassy '{request.Service.Embassy.Name}' is already being processed."
                        |> Ok
                        |> async.Return
                    | Ready
                    | Failed _
                    | Completed _ ->
                        deps.processRequest request
                        |> ResultAsync.bind (fun result -> (deps.Chat.Id, deps.printPayload) |> Notification.create result)
                | None ->
                    resultAsync {
                        let! service = deps.getService model.ServiceId
                        let! embassy = deps.getEmbassy model.EmbassyId
                        let! request = deps.createRequest (model.Payload, service, embassy, false, Disabled)

                        return
                            deps.processRequest request
                            |> ResultAsync.bind (fun result ->
                                (deps.Chat.Id, deps.printPayload) |> Notification.create result)
                    }
        }

let sendAppointments (model: Post.Model.SendAppointments) =
    fun (deps: Embassies.Dependencies) ->
        deps.getChatRequests ()
        |> ResultAsync.map (
            List.filter (fun request ->
                request.Service.Id = model.ServiceId
                && request.Service.Embassy.Id = model.EmbassyId)
        )
        |> ResultAsync.bind (
            Seq.map (fun r -> (deps.Chat.Id, deps.printPayload) |> Notification.create r)
            >> Result.choose
        )

let confirmAppointment (model: Post.Model.ConfirmAppointment) =
    fun (deps: Embassies.Dependencies) ->
        deps.getRequest model.RequestId
        |> ResultAsync.bindAsync (fun request ->
            deps.processRequest {
                request with
                    ConfirmationState = Appointment model.AppointmentId
            })
        |> ResultAsync.bind (fun r -> (deps.Chat.Id, deps.printPayload) |> Notification.create r)

let deleteSubscription requestId =
    fun (deps: Embassies.Dependencies) ->
        deps.getRequest requestId
        |> ResultAsync.bindAsync (fun request ->
            match request.ProcessState with
            | InProcess ->
                (deps.Chat.Id, New)
                |> Text.create
                    $"The request for the service '{request.Service.Name}' for the embassy '{request.Service.Embassy.Name}' is still being processed."
                |> Ok
                |> async.Return
            | Ready
            | Failed _
            | Completed _ ->
                deps.deleteRequest requestId
                |> ResultAsync.bind (fun _ ->
                    (deps.Chat.Id, New)
                    |> Text.create $"Subscription '{requestId}' for '{request.Service.Name}' has been deleted."
                    |> Ok))
