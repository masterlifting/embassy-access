module EA.Telegram.Services.Consumer.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram.Endpoints.Consumer.Request
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Embassies.Russian
open EA.Embassies.Russian.Domain

module Kdmid =
    open EA.Embassies.Russian.Kdmid.Domain
    open EA.Embassies.Russian.Kdmid.Dependencies
    open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Model.Kdmid

    module Instruction =

        let private toResponse instruction route =
            fun (chatId, messageId) ->

                let message = $"{route}{String.addLines 2}"

                instruction
                |> Option.map (fun instr -> message + $"Инструкция:{String.addLines 2}{instr}")
                |> Option.defaultValue message
                |> fun message -> (chatId, messageId |> Replace) |> Text.create message

        let private toSubscribe embassyId (service: ServiceNode) confirmation =
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
                |> toResponse service.Instruction request.Value
                |> Ok
                |> async.Return

        let private toCheckAppointments embassyId (service: ServiceNode) =
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
                |> toResponse service.Instruction request.Value
                |> Ok
                |> async.Return

        let private toStandardSubscribe embassyId service =
            fun (deps: Russian.Dependencies) -> (deps.ChatId, deps.MessageId) |> toSubscribe embassyId service Disabled

        let private toFirstAvailableAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe embassyId service (ConfirmationState.Auto <| FirstAvailable)

        let private toLastAvailableAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe embassyId service (ConfirmationState.Auto <| LastAvailable)

        let private toDateRangeAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe
                    embassyId
                    service
                    (ConfirmationState.Auto <| DateTimeRange(DateTime.MinValue, DateTime.MaxValue))

        let create embassyId (service: ServiceNode) =
            fun (deps: Russian.Dependencies) ->
                match service.Id.Value |> Graph.split with
                | [ _; "RU"; _; _; "0" ] -> deps |> toCheckAppointments embassyId service
                | [ _; "RU"; _; _; "1" ] -> deps |> toStandardSubscribe embassyId service
                | [ _; "RU"; _; _; "2"; "0" ] -> deps |> toFirstAvailableAutoSubscribe embassyId service
                | [ _; "RU"; _; _; "2"; "1" ] -> deps |> toLastAvailableAutoSubscribe embassyId service
                | [ _; "RU"; _; _; "2"; "2" ] -> deps |> toDateRangeAutoSubscribe embassyId service
                | _ -> service.ShortName |> NotSupported |> Error |> async.Return

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

    let sendAppointments (model: SendAppointments) =
        fun (deps: Russian.Dependencies) ->
            deps.getChatRequests ()
            |> ResultAsync.map (
                List.filter (fun request ->
                    request.Service.Id = model.ServiceId
                    && request.Service.Embassy.Id = model.EmbassyId)
            )
            |> ResultAsync.bind (fun requests ->
                requests
                |> Seq.map (fun request ->
                    request.Service.Payload
                    |> Payload.toValue
                    |> Result.map (fun payloadValue ->
                        request.Appointments
                        |> Set.filter (fun x -> model.AppointmentIds |> Set.contains x.Id)
                        |> Seq.map (fun appointment ->
                            let route =
                                RussianEmbassy(
                                    Post(
                                        KdmidConfirmAppointment(
                                            { RequestId = request.Id
                                              AppointmentId = appointment.Id }
                                        )
                                    )
                                )

                            let buttonName = $"{appointment.Description} ({payloadValue})"

                            route.Value, buttonName)
                        |> Map
                        |> fun buttons ->
                            (deps.ChatId, New)
                            |> Buttons.create
                                { Name = $"Choose the appointment for '{request.Service.Embassy.ShortName}'"
                                  Columns = 1
                                  Data = buttons }))
                |> Result.choose)

    let confirmAppointment (model: ConfirmAppointment) =
        fun (deps: Russian.Dependencies) ->
            deps.getRequest model.RequestId
            |> ResultAsync.bindAsync (fun request ->
                deps
                |> getService
                    { request with
                        ConfirmationState = ConfirmationState.Manual model.AppointmentId })
            |> ResultAsync.map (fun result -> deps.ChatId |> createNotification result)

module Midpass =
    open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Model.Midpass

    let checkStatus (_: CheckStatus) =
        fun (_: Russian.Dependencies) -> "checkStatus" |> NotImplemented |> Error |> async.Return
