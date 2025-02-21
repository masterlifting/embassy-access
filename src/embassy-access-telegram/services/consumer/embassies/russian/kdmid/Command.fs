module EA.Telegram.Services.Consumer.Embassies.Russian.Kdmid.Command

open System
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain
open EA.Telegram.Endpoints.Embassies.Russian
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian.Service.Kdmid

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
                    $"Подписка на услугу '{request.Service.Name}' для посольства '{request.Service.Embassy.Name}' уже существует."
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
                            $"Подписка '{request.Id.ValueStr}' на услугу '{service.Name}' для посольства '{embassy.Name}' создана."
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
                            $"Запрос на услугу '{request.Service.Name}' для посольства '{request.Service.Embassy.Name}' уже в обработке."
                        |> Ok
                        |> async.Return
                    | Ready
                    | Failed _
                    | Completed _ ->
                        deps
                        |> Request.getService request
                        |> ResultAsync.bind (fun result -> deps.Chat.Id |> Request.toResponse result)
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
                            deps
                            |> Request.getService request
                            |> ResultAsync.bind (fun result -> deps.Chat.Id |> Request.toResponse result)
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
        |> ResultAsync.bind (Seq.map (fun r -> deps.Chat.Id |> Request.toResponse r) >> Result.choose)

let confirmAppointment (model: Kdmid.Post.Model.ConfirmAppointment) =
    fun (deps: Kdmid.Dependencies) ->
        deps.getRequest model.RequestId
        |> ResultAsync.bindAsync (fun request ->
            deps
            |> Request.getService
                { request with
                    ConfirmationState = ConfirmationState.Manual model.AppointmentId })
        |> ResultAsync.bind (fun r -> deps.Chat.Id |> Request.toResponse r)

let deleteSubscription requestId =
    fun (deps: Kdmid.Dependencies) ->
        deps.getRequest requestId
        |> ResultAsync.bindAsync (fun request ->
            match request.ProcessState with
            | InProcess ->
                (deps.Chat.Id, New)
                |> Text.create
                    $"Запрос на услугу '{request.Service.Name}' для посольства '{request.Service.Embassy.Name}' еще в обработке."
                |> Ok
                |> async.Return
            | Ready
            | Failed _
            | Completed _ ->
                deps.deleteRequest requestId
                |> ResultAsync.bind (fun _ ->
                    (deps.Chat.Id, New)
                    |> Text.create $"Подписка для '{request.Service.Name}' удалена"
                    |> Ok))
