[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Embassies.Russian

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Endpoints.Consumer.Embassies.Russian
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Embassies.Russian.Kdmid.Domain

module internal Get =
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
                    EA.Telegram.Endpoints.Consumer.Core.RussianEmbassy(
                        Post(
                            PostRequest.KdmidSubscribe(
                                { Confirmation = confirmation
                                  ServiceId = service.Id
                                  EmbassyId = embassyId
                                  Payload = "ссылку вставить сюда" }
                            )
                        )
                    )

                (chatId, messageId)
                |> createInstruction service.Instruction request.Route
                |> Ok
                |> async.Return

        let toCheckAppointments embassyId (service: ServiceNode) =
            fun (deps: Russian.Dependencies) ->
                let request =
                    EA.Telegram.Endpoints.Consumer.Core.RussianEmbassy(
                        Post(
                            PostRequest.KdmidCheckAppointments(
                                { ServiceId = service.Id
                                  EmbassyId = embassyId
                                  Payload = "ссылку вставить сюда" }
                            )
                        )
                    )

                (deps.ChatId, deps.MessageId)
                |> createInstruction service.Instruction request.Route
                |> Ok
                |> async.Return

        let toStandardSubscribe embassyId service =
            fun (deps: Russian.Dependencies) -> (deps.ChatId, deps.MessageId) |> toSubscribe embassyId service Disabled

        let toFirstAvailableAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe embassyId service (Auto FirstAvailable)

        let toLastAvailableAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe embassyId service (Auto LastAvailable)

        let toDateRangeAutoSubscribe embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> toSubscribe embassyId service (Auto(DateTimeRange(DateTime.MinValue, DateTime.MaxValue)))

    let toResponse embassyId (service: ServiceNode) =
        fun (deps: Russian.Dependencies) ->
            let idParts = service.Id.Value |> Graph.split

            match idParts with
            | [ _; "RU"; _; _; "0" ] -> deps |> Kdmid.toCheckAppointments embassyId service
            | [ _; "RU"; _; _; "1" ] -> deps |> Kdmid.toStandardSubscribe embassyId service
            | [ _; "RU"; _; _; "2"; "0" ] -> deps |> Kdmid.toFirstAvailableAutoSubscribe embassyId service
            | [ _; "RU"; _; _; "2"; "1" ] -> deps |> Kdmid.toLastAvailableAutoSubscribe embassyId service
            | [ _; "RU"; _; _; "2"; "2" ] -> deps |> Kdmid.toDateRangeAutoSubscribe embassyId service
            | _ -> service.ShortName |> NotSupported |> Error |> async.Return

module internal Post =
    module private Midpass =
        open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Model.Midpass

        let checkStatus (model: CheckStatus) =
            fun (deps: Russian.Dependencies) -> "checkStatus" |> NotImplemented |> Error |> async.Return

    module private Kdmid =
        open EA.Embassies.Russian
        open EA.Embassies.Russian.Domain
        open EA.Embassies.Russian.Kdmid.Dependencies
        open EA.Telegram.Endpoints.Consumer.Embassies.Russian.Model.Kdmid

        let private createCoreRequest (kdmidRequest: KdmidRequest) =
            fun (deps: Russian.Dependencies) ->
                let request = kdmidRequest.ToNewCoreRequest()

                deps.createOrUpdateChat
                    { Id = deps.ChatId
                      Subscriptions = [ request.Id ] |> Set }
                |> ResultAsync.bindAsync (fun _ -> request |> deps.createOrUpdateRequest)

        let private createRequest embassy service payload confirmation =
            payload
            |> Web.Http.Route.toUri
            |> Result.map (fun uri ->
                { Uri = uri
                  Service = service
                  Embassy = embassy
                  Confirmation = confirmation })
            |> async.Return

        let private getService request =
            fun (deps: Russian.Dependencies) ->
                { Request = request
                  Dependencies = Order.Dependencies.create deps.RequestStorage deps.CancellationToken }
                |> Kdmid
                |> API.Service.get

        let checkAppointments (model: CheckAppointments) =
            fun (deps: Russian.Dependencies) ->
                let inline createNotification request =
                    fun chatId ->
                        let errorFilter error = true

                        Notification.tryCreate errorFilter request
                        |> Option.map _.Message
                        |> Option.defaultValue
                            $"Не удалось создать уведомление для запроса {request.Id} и результата {request.ProcessState}"
                        |> fun message -> (chatId, New) |> Text.create message

                let result = ResultAsyncBuilder()

                result {

                    let! requestOpt =
                        deps.getEmbassyRequests model.EmbassyId
                        |> ResultAsync.map (
                            List.tryFind (fun request ->
                                request.Service.Id = model.ServiceId && request.Service.Payload = model.Payload)
                        )

                    let! result =
                        match requestOpt with
                        | Some request -> deps |> getService request
                        | None ->
                            result {
                                let! service = deps.getServiceNode model.ServiceId
                                let! embassy = deps.getEmbassyNode model.EmbassyId
                                let! kdmidRequest = createRequest embassy service model.Payload Disabled
                                let! request = deps |> createCoreRequest kdmidRequest
                                return deps |> getService request
                            }

                    let notification = deps.ChatId |> createNotification result
                    return notification |> Ok |> async.Return
                }

        let subscribe (model: Subscribe) =
            fun (deps: Russian.Dependencies) ->
                let result = ResultAsyncBuilder()

                result {
                    let! service = deps.getServiceNode model.ServiceId
                    let! embassy = deps.getEmbassyNode model.EmbassyId
                    let! kdmidRequest = createRequest embassy service model.Payload model.Confirmation
                    let! request = deps |> createCoreRequest kdmidRequest
                    let! result = deps.createOrUpdateRequest request

                    let notification =
                        (deps.ChatId, New)
                        |> Text.create
                            $"Подписка на услугу {result.Service.Name} для посольства {result.Service.Embassy.ShortName} создана"

                    return notification |> Ok |> async.Return
                }

        let confirm (model: Confirm) =
            fun (deps: Russian.Dependencies) ->

                let inline createNotification request =
                    fun chatId ->
                        let errorFilter error = true

                        Notification.tryCreate errorFilter request
                        |> Option.map _.Message
                        |> Option.defaultValue
                            $"Не удалось создать уведомление для запроса {request.Id} и результата {request.ProcessState}"
                        |> fun message -> (chatId, New) |> Text.create message

                let result = ResultAsyncBuilder()

                result {
                    let! request = deps.getRequest model.RequestId

                    let request =
                        { request with
                            ConfirmationState = Manual model.AppointmentId }

                    let! result = deps |> getService request
                    let notification = deps.ChatId |> createNotification result
                    return notification |> Ok |> async.Return
                }

    let toResponse request =
        fun (deps: Russian.Dependencies) ->
            match request with
            | KdmidCheckAppointments model -> deps |> Kdmid.checkAppointments model
            | KdmidSubscribe model -> deps |> Kdmid.subscribe model
            | KdmidConfirm model -> deps |> Kdmid.confirm model
            | MidpassCheckStatus model -> deps |> Midpass.checkStatus model

let toResponse request =
    fun (deps: EA.Telegram.Dependencies.Consumer.Core.Dependencies) ->
        Russian.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Post postRequest -> deps |> Post.toResponse postRequest)
