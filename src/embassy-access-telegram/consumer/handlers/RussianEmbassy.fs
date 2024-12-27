[<RequireQualifiedAccess>]
module EA.Telegram.Consumer.Handlers.RussianEmbassy

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain
open EA.Telegram.Consumer.Dependencies
open EA.Telegram.Consumer.Endpoints
open EA.Telegram.Consumer.Endpoints.RussianEmbassy

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
        fun (deps: RussianEmbassy.Dependencies) -> model.Number |> NotSupported |> Error |> async.Return

module private Kdmid =
    open EA.Embassies.Russian.Domain
    open EA.Embassies.Russian.Kdmid.Dependencies
    open EA.Embassies.Russian

    let createInstruction embassyId (service: ServiceNode) =
        fun (deps: RussianEmbassy.Dependencies) ->
            let request =
                Core.RussianEmbassy(
                    Post(
                        PostRequest.Kdmid(
                            { ServiceId = service.Id
                              EmbassyId = embassyId
                              Payload = "{вставить сюда}" }
                        )
                    )
                )

            let message = $"%s{request.Route}%s{String.addLines 2}"

            service.Instruction
            |> Option.map (fun instruction -> message + $"Инструкция:%s{String.addLines 2}%s{instruction}")
            |> Option.defaultValue message
            |> Text.create
            |> fun create -> (deps.ChatId, deps.MessageId |> Replace) |> create

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

    let createRequest (kdmidRequest: KdmidRequest) =
        fun (deps: RussianEmbassy.Dependencies) ->
            let request = kdmidRequest.CreateRequest()

            deps.createOrUpdateChat
                { Id = deps.ChatId
                  Subscriptions = [ request.Id ] |> Set }
            |> ResultAsync.bindAsync (fun _ -> request |> deps.createOrUpdateRequest)

    let getService timeZone request =
        fun (deps: RussianEmbassy.Dependencies) ->
            deps.initRequestStorage ()
            |> Result.map (fun requestStorage -> Order.Dependencies.create requestStorage deps.CancellationToken)
            |> Result.map (fun deps ->
                { Order =
                    { Request = request
                      TimeZone = timeZone }
                  Dependencies = deps }
                |> Kdmid)
            |> ResultAsync.wrap API.Service.get

    let post (model: KdmidPostModel) =
        fun (deps: RussianEmbassy.Dependencies) ->
            let result = ResultAsyncBuilder()
            result {
                let! service = deps.getService model.ServiceId
                let! embassy = deps.getEmbassy model.EmbassyId
                let! kdmidRequest = createKdmidRequest embassy service model.Payload
                let! request =  deps |> createRequest kdmidRequest
                let! result = deps |> getService kdmidRequest.TimeZone request
                let message = createMessage result
                let tgResponse = (deps.ChatId, New) |> Text.create message 
                return tgResponse |> Ok |> async.Return
            }

let internal getService embassyId (service: ServiceNode) =
    fun (deps: RussianEmbassy.Dependencies) ->
        match service.Id.Value with
        | "SRV.RU.0.1" -> service.Name |> NotSupported |> Error |> async.Return
        | _ -> deps |> Kdmid.createInstruction embassyId service |> Ok |> async.Return

let toResponse request =
    fun (deps: Core.Dependencies) ->
        RussianEmbassy.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Post postRequest ->
                match postRequest with
                | Kdmid model -> deps |> Kdmid.post model
                | Midpass model -> deps |> Midpass.post model)
