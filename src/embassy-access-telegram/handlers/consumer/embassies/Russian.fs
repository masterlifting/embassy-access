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

        let toImmediateResult embassyId (service: ServiceNode) =
            fun (deps: Russian.Dependencies) ->
                let request =
                    EA.Telegram.Endpoints.Consumer.Core.RussianEmbassy(
                        Post(
                            PostRequest.Kdmid(
                                { Confirmation = Disabled
                                  ServiceId = service.Id
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
                |> Ok
                |> async.Return

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

    let post (model: KdmidPostModel) =
        fun (deps: Russian.Dependencies) ->
            let result = ResultAsyncBuilder()

            result {
                let! service = deps.getServiceNode model.ServiceId
                let! embassy = deps.getEmbassyNode model.EmbassyId
                let! kdmidRequest = createKdmidRequest embassy service model.Payload
                let! request = deps |> createRequest kdmidRequest
                let! result = deps |> getService kdmidRequest.TimeZone request
                let message = createMessage result
                let tgResponse = (deps.ChatId, New) |> Text.create message
                return tgResponse |> Ok |> async.Return
            }

let internal getService embassyId (service: ServiceNode) =
    fun (deps: Russian.Dependencies) ->
        let idParts = service.Id.Value |> Graph.split

        match idParts with
        | [ _; "RU"; _; _; "0" ] -> deps |> Kdmid.Instructions.toImmediateResult embassyId service
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
