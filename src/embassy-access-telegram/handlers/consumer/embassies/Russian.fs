[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Embassies.Russian.Kdmid.Domain

module internal Get =
    open EA.Telegram.Endpoints.Consumer.Request

    module private Kdmid =

        open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Model.Kdmid

        let inline private createInstruction instruction route =
            fun (chatId, messageId) ->

                let message = $"{route}{String.addLines 2}"

                instruction
                |> Option.map (fun instr -> message + $"Инструкция:{String.addLines 2}{instr}")
                |> Option.defaultValue message
                |> fun message -> (chatId, messageId |> Replace) |> Text.create message

        let inline private toSubscribe embassyId (service: ServiceNode) confirmation =
            fun (chatId, messageId) ->
                let request =
                    RussianEmbassy(
                        Post(
                            KdmidSubscribe(
                                { ConfirmationState = confirmation
                                  ServiceId = service.Id
                                  EmbassyId = embassyId
                                  Payload = "ссылку вставить сюда" }
                            )
                        )
                    )

                (chatId, messageId)
                |> createInstruction service.Instruction request.Value
                |> Ok
                |> async.Return

        let toCheckAppointments embassyId (service: ServiceNode) =
            fun (deps: Russian.Dependencies) ->
                let request =
                    RussianEmbassy(
                        Post(
                            KdmidCheckAppointments(
                                { ServiceId = service.Id
                                  EmbassyId = embassyId
                                  Payload = "ссылку вставить сюда" }
                            )
                        )
                    )

                (deps.ChatId, deps.MessageId)
                |> createInstruction service.Instruction request.Value
                |> Ok
                |> async.Return

        let toStandardSubscribe embassyId service =
            fun (deps: Russian.Dependencies) -> (deps.ChatId, deps.MessageId) |> toSubscribe embassyId service Disabled

        let toFirstAvailableAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe embassyId service (ConfirmationState.Auto <| FirstAvailable)

        let toLastAvailableAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe embassyId service (ConfirmationState.Auto <| LastAvailable)

        let toDateRangeAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe
                    embassyId
                    service
                    (ConfirmationState.Auto <| DateTimeRange(DateTime.MinValue, DateTime.MaxValue))

    let toResponse embassyId (service: ServiceNode) =
        fun (deps: Russian.Dependencies) ->
            match service.Id.Value |> Graph.split with
            | [ _; "RU"; _; _; "0" ] -> deps |> Kdmid.toCheckAppointments embassyId service
            | [ _; "RU"; _; _; "1" ] -> deps |> Kdmid.toStandardSubscribe embassyId service
            | [ _; "RU"; _; _; "2"; "0" ] -> deps |> Kdmid.toFirstAvailableAutoSubscribe embassyId service
            | [ _; "RU"; _; _; "2"; "1" ] -> deps |> Kdmid.toLastAvailableAutoSubscribe embassyId service
            | [ _; "RU"; _; _; "2"; "2" ] -> deps |> Kdmid.toDateRangeAutoSubscribe embassyId service
            | _ -> service.ShortName |> NotSupported |> Error |> async.Return

module internal Post =
    module Midpass =
        open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Model.Midpass

        let checkStatus (_: CheckStatus) =
            fun (_: Russian.Dependencies) -> "checkStatus" |> NotImplemented |> Error |> async.Return

    module Kdmid =
        open EA.Embassies.Russian
        open EA.Embassies.Russian.Domain
        open EA.Embassies.Russian.Kdmid.Dependencies
        open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Model.Kdmid

        let private createNotification request =
            fun chatId ->
                let errorFilter _ = true

                Notification.tryCreate errorFilter request
                |> Option.map _.Message
                |> Option.defaultValue
                    $"Не удалось создать уведомление для запроса {request.Id} и результата {request.ProcessState}"
                |> fun message -> (chatId, New) |> Text.create message

        let private createCoreRequest (kdmidRequest: KdmidRequest) =
            fun (deps: Russian.Dependencies) ->
                let request = kdmidRequest.ToRequest()

                deps.createChatSubscription request.Id
                |> ResultAsync.bindAsync (fun _ -> request |> deps.createRequest)

        let private getService request =
            fun (deps: Russian.Dependencies) ->
                { Request = request
                  Dependencies = Order.Dependencies.create deps.RequestStorage deps.CancellationToken }
                |> Kdmid
                |> API.Service.get

        let subscribe (model: Subscribe) =
            fun (deps: Russian.Dependencies) ->
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
                                    |> ResultAsync.wrap (fun kdmidRequest -> deps |> createCoreRequest kdmidRequest)

                                return
                                    $"Подписка '{request.Id.ValueStr}' на услугу '{service.Name}' для посольства '{embassy.Name}' создана."
                                    |> Ok
                                    |> async.Return
                            }

                    return (deps.ChatId, New) |> Text.create message |> Ok |> async.Return
                }

        let checkAppointments (model: CheckAppointments) =
            fun (deps: Russian.Dependencies) ->
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
                                (deps.ChatId, New)
                                |> Text.create
                                    $"Запрос на услугу '{request.Service.Name}' для посольства '{request.Service.Embassy.Name}' уже в обработке."
                                |> Ok
                                |> async.Return
                            | _ ->
                                deps
                                |> getService request
                                |> ResultAsync.map (fun result -> deps.ChatId |> createNotification result)
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
                                    |> ResultAsync.wrap (fun kdmidRequest -> deps |> createCoreRequest kdmidRequest)

                                return
                                    deps
                                    |> getService request
                                    |> ResultAsync.map (fun result -> deps.ChatId |> createNotification result)
                            }
                }

        let confirmAppointment (model: ConfirmAppointment) =
            fun (deps: Russian.Dependencies) ->
                deps.getRequest model.RequestId
                |> ResultAsync.bindAsync (fun request ->
                    deps
                    |> getService
                        { request with
                            ConfirmationState = ConfirmationState.Manual model.AppointmentId })
                |> ResultAsync.map (fun result -> deps.ChatId |> createNotification result)

let toResponse request =
    fun (deps: EA.Telegram.Dependencies.Consumer.Core.Dependencies) ->
        Russian.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Post post ->
                match post with
                | KdmidSubscribe model -> deps |> Post.Kdmid.subscribe model
                | KdmidCheckAppointments model -> deps |> Post.Kdmid.checkAppointments model
                | KdmidConfirmAppointment model -> deps |> Post.Kdmid.confirmAppointment model
                | MidpassCheckStatus model -> deps |> Post.Midpass.checkStatus model)
