module EA.Telegram.Services.Embassies.Russian.Kdmid.Command

open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain
open EA.Telegram.Endpoints.Embassies.Russian
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian.Kdmid

let subscribe (model: Kdmid.Post.Model.Subscribe) =
    fun (deps: Kdmid.Dependencies) ->
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
                            model.Payload
                            |> Web.Http.Route.toUri
                            |> Result.map (fun uri ->
                                { Uri = uri
                                  Service = service
                                  Embassy = embassy
                                  SubscriptionState = Auto
                                  ConfirmationState = model.ConfirmationState })
                            |> Result.map _.ToRequest()
                            |> ResultAsync.wrap deps.createRequest

                        return
                            $"Subscription '{request.Id.ValueStr}' for the service '{service.Name}' for the embassy '{embassy.Name}' has been created."
                            |> Ok
                            |> async.Return
                    }

            return (deps.Chat.Id, New) |> Text.create message |> Ok |> async.Return
        }

let checkAppointments (model: Kdmid.Post.Model.CheckAppointments) =
    fun (deps: Kdmid.Dependencies) ->
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
                        deps.getApi request
                        |> ResultAsync.bind (fun result -> deps.Chat.Id |> Message.Notification.create result)
                | None ->
                    resultAsync {
                        let! service = deps.getService model.ServiceId
                        let! embassy = deps.getEmbassy model.EmbassyId

                        let! request =
                            model.Payload
                            |> Web.Http.Route.toUri
                            |> Result.map (fun uri ->
                                { Uri = uri
                                  Service = service
                                  Embassy = embassy
                                  SubscriptionState = Manual
                                  ConfirmationState = Disabled })
                            |> Result.map _.ToRequest()
                            |> ResultAsync.wrap deps.createRequest

                        return
                            deps.getApi request
                            |> ResultAsync.bind (fun result -> deps.Chat.Id |> Message.Notification.create result)
                    }
        }

let sendAppointments (model: Kdmid.Post.Model.SendAppointments) =
    fun (deps: Kdmid.Dependencies) ->
        deps.getChatRequests ()
        |> ResultAsync.map (
            List.filter (fun request ->
                request.Service.Id = model.ServiceId
                && request.Service.Embassy.Id = model.EmbassyId)
        )
        |> ResultAsync.bind (
            Seq.map (fun r -> deps.Chat.Id |> Message.Notification.create r)
            >> Result.choose
        )

let confirmAppointment (model: Kdmid.Post.Model.ConfirmAppointment) =
    fun (deps: Kdmid.Dependencies) ->
        deps.getRequest model.RequestId
        |> ResultAsync.bindAsync (fun request ->
            deps.getApi
                { request with
                    ConfirmationState = ConfirmationState.Manual model.AppointmentId })
        |> ResultAsync.bind (fun r -> deps.Chat.Id |> Message.Notification.create r)

let deleteSubscription requestId =
    fun (deps: Kdmid.Dependencies) ->
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
                    |> Text.create $"Subscription for '{request.Service.Name}' has been deleted"
                    |> Ok))
