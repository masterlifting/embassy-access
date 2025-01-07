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

let private createMessage request =
    let errorFilter error = true

    Notification.tryCreate errorFilter request
    |> Option.map _.Message
    |> Option.defaultValue
        $"Не удалось создать уведомление для запроса {request.Id} и результата {request.ProcessState}"

module private Midpass =

    let pickService (serviceNode: Graph.Node<ServiceNode>) embassy payload =
        fun deps -> serviceNode.ShortName |> NotSupported |> Error |> async.Return

    let post (model: MidpassPostModel) =
        fun (deps: Russian.Dependencies) -> model.Number |> NotSupported |> Error |> async.Return

module private Kdmid =
    open EA.Embassies.Russian.Domain
    open EA.Embassies.Russian.Kdmid.Dependencies
    open EA.Embassies.Russian

    module Instructions =

        let inline private createPostRequest embassyId (service: ServiceNode) confirmation =
            fun (chatId, messageId) ->
                let request =
                    EA.Telegram.Endpoints.Consumer.Core.RussianEmbassy(
                        Post(
                            PostRequest.Kdmid(
                                { Confirmation = confirmation
                                  ServiceId = service.Id
                                  EmbassyId = embassyId
                                  Payload = "ссылку вставить сюда" }
                            )
                        )
                    )

                let message = $"{request.Route}{String.addLines 2}"

                service.Instruction
                |> Option.map (fun instr -> message + $"Инструкция:{String.addLines 2}{instr}")
                |> Option.defaultValue message
                |> Text.create
                |> fun create -> (chatId, messageId |> Replace) |> create
                |> Ok
                |> async.Return

        let toImmediateResult embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId) |> createPostRequest embassyId service None

        let toStandardSubscription embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> createPostRequest embassyId service (Some(Disabled))

        let toFirstAvailableAutoSubscription embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> createPostRequest embassyId service (Some(Auto FirstAvailable))

        let toLastAvailableAutoSubscription embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> createPostRequest embassyId service (Some(Auto LastAvailable))

        let toDateRangeAutoSubscription embassyId service =
            fun (deps: Russian.Dependencies) ->
                (deps.ChatId, deps.MessageId)
                |> createPostRequest embassyId service (Some(Auto(DateTimeRange(DateTime.MinValue, DateTime.MaxValue))))

    module Actions =
        let createKdmidRequest embassy service payload =
            payload
            |> Web.Http.Route.toUri
            |> Result.map (fun uri ->
                { Uri = uri
                  Service = service
                  Embassy = embassy
                  TimeZone = 1.0
                  Confirmation = Disabled })
            |> async.Return

        let createCoreRequest (kdmidRequest: KdmidRequest) =
            fun (deps: Russian.Dependencies) ->
                let request = kdmidRequest.CreateRequest()

                deps.createOrUpdateChat
                    { Id = deps.ChatId
                      Subscriptions = [ request.Id ] |> Set }
                |> ResultAsync.bindAsync (fun _ -> request |> deps.createOrUpdateRequest)

        let getService timeZone request =
            fun (deps: Russian.Dependencies) ->
                { Order =
                    { Request = request
                      TimeZone = timeZone }
                  Dependencies = Order.Dependencies.create deps.RequestStorage deps.CancellationToken }
                |> Kdmid
                |> API.Service.get

        let getImmediateResult (model: KdmidPostModel) =
            fun (deps: Russian.Dependencies) ->
                let result = ResultAsyncBuilder()

                result {
                    let! service = deps.getServiceNode model.ServiceId
                    let! embassy = deps.getEmbassyNode model.EmbassyId
                    let! kdmidRequest = createKdmidRequest embassy service model.Payload
                    let! request = deps |> createCoreRequest kdmidRequest
                    let! result = deps |> getService kdmidRequest.TimeZone request
                    return result |> Ok |> async.Return
                }

    let post (model: KdmidPostModel) =
        fun (deps: Russian.Dependencies) ->
            match model.Confirmation with
            | None ->
                deps
                |> Actions.getImmediateResult model
                |> ResultAsync.map createMessage
                |> ResultAsync.map (fun message -> (deps.ChatId, New) |> Text.create message)
            | Some _ -> "Request" |> NotSupported |> Error |> async.Return

let internal getService embassyId (service: ServiceNode) =
    fun (deps: Russian.Dependencies) ->
        let idParts = service.Id.Value |> Graph.split

        match idParts with
        | [ _; "RU"; _; _; "0" ] -> deps |> Kdmid.Instructions.toImmediateResult embassyId service
        | [ _; "RU"; _; _; "1" ] -> deps |> Kdmid.Instructions.toStandardSubscription embassyId service
        | [ _; "RU"; _; _; "2"; "0" ] -> deps |> Kdmid.Instructions.toFirstAvailableAutoSubscription embassyId service
        | [ _; "RU"; _; _; "2"; "1" ] -> deps |> Kdmid.Instructions.toLastAvailableAutoSubscription embassyId service
        | [ _; "RU"; _; _; "2"; "2" ] -> deps |> Kdmid.Instructions.toDateRangeAutoSubscription embassyId service
        | _ -> service.ShortName |> NotSupported |> Error |> async.Return

let toResponse request =
    fun (deps: EA.Telegram.Dependencies.Consumer.Core.Dependencies) ->
        Russian.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Post postRequest ->
                match postRequest with
                | Kdmid model -> deps |> Kdmid.post model
                | Midpass model -> deps |> Midpass.post model)
