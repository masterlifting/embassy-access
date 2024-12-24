[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Consumer.Services.Russian

open System
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain
open EA.Telegram.Routes
open EA.Telegram.Routes.Services
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Routes.Russian

let private createMessage request =
    let errorFilter error = true

    Notification.tryCreate errorFilter request
    |> Option.map _.Message
    |> Option.defaultValue
        $"Не удалось создать уведомление для запроса {request.Id} и результата {request.ProcessState}"

module private Midpass =

    let pickService (node: Graph.Node<ServiceNode>) embassy payload =
        fun deps -> node.ShortName |> NotSupported |> Error |> async.Return

    let post (model: MidpassPostModel) =
        fun (deps: Russian.Dependencies) -> model.Number |> NotSupported |> Error |> async.Return

module private Kdmid =
    open EA.Embassies.Russian.Domain
    open EA.Embassies.Russian.Kdmid.Dependencies

    let createInstruction (embassy: EmbassyNode) (service: ServiceNode) =
        fun (deps: Russian.Dependencies) ->
            let request =
                Router.Russian(
                    Post(
                        PostRequest.Kdmid(
                            { ServiceId = service.Id
                              EmbassyId = embassy.Id
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

    let post (model: KdmidPostModel) =
        fun (deps: Russian.Dependencies) -> model.ServiceId.Value |> NotSupported |> Error |> async.Return

    let createKdmidRequest embassy payload =
        payload
        |> Web.Http.Route.toUri
        |> Result.map (fun uri ->
            { Uri = uri
              Embassy = embassy
              TimeZone = 1.0
              Confirmation = Disabled })

    let createRequest serviceName (kdmidRequest: KdmidRequest) =
        fun (deps: Russian.Dependencies) ->
            let request = kdmidRequest.CreateRequest serviceName

            deps.createOrUpdateChat
                { Id = deps.ChatId
                  Subscriptions = [ request.Id ] |> Set }
            |> ResultAsync.bindAsync (fun _ -> request |> deps.createOrUpdateRequest)

    let getService timeZone request =
        fun (deps: Russian.Dependencies) ->
            deps.initRequestStorage ()
            |> Result.map (fun requestStorage -> Order.Dependencies.create requestStorage deps.CancellationToken)
            |> Result.map (fun deps ->
                { Order =
                    { Request = request
                      TimeZone = timeZone }
                  Dependencies = deps }
                |> Kdmid)
            |> ResultAsync.wrap EA.Embassies.Russian.API.Service.get

    let pickService (node: Graph.Node<ServiceNode>) embassy payload =
        fun (deps: Russian.Dependencies) ->
            match node.FullIds |> Seq.map _.Value |> Seq.last with
            | "20" ->
                deps.getChatRequests ()
                |> ResultAsync.bindAsync (fun requests ->
                    match requests with
                    | [] ->
                        createKdmidRequest embassy payload
                        |> ResultAsync.wrap (fun kdmidRequest ->
                            deps
                            |> createRequest node.Value.Name kdmidRequest
                            |> ResultAsync.bindAsync (fun request ->
                                deps
                                |> getService kdmidRequest.TimeZone request
                                |> ResultAsync.map createMessage
                                |> ResultAsync.map Text.create
                                |> ResultAsync.map (fun create -> create (deps.ChatId, New))))
                    | requests ->
                        match requests |> List.tryFind (fun request -> request.Id.ValueStr = payload) with
                        | Some request ->
                            deps
                            |> getService 1 request
                            |> ResultAsync.map createMessage
                            |> ResultAsync.map Text.create
                            |> ResultAsync.map (fun create -> create (deps.ChatId, New))
                        | None ->
                            let command =
                                (embassy.Id, node.Value.Id, "{вставить сюда}")
                                |> Command.SetService
                                |> Command.set

                            let doubleLine = Environment.NewLine + Environment.NewLine
                            let message = $"%s{command}%s{doubleLine}"

                            node.Value.Instruction
                            |> Option.map (fun instruction -> message + $"Инструкция:%s{doubleLine}%s{instruction}")
                            |> Option.defaultValue message
                            |> Text.create
                            |> fun create -> (deps.ChatId, deps.MessageId |> Replace) |> create |> Ok |> async.Return)
            | _ -> node.ShortName |> NotSupported |> Error |> async.Return

let pickService (node: Graph.Node<ServiceNode>) embassy payload =
    fun deps ->

        let nodeId =
            match node.FullId with
            | Graph.NodeIdValue value -> value |> Graph.split

        match nodeId.Length with
        | length when length > 2 ->
            match nodeId[0] with
            | "RU" ->
                match nodeId[1], nodeId[2] with
                | "1", "3" -> deps |> Midpass.pickService node embassy payload
                | _ -> deps |> Kdmid.pickService node embassy payload
            | _ -> node.ShortName |> NotSupported |> Error |> async.Return
        | _ -> node.ShortName |> NotSupported |> Error |> async.Return

let private createButtons chatId msgIdOpt name data =
    (chatId, msgIdOpt |> Option.map Replace |> Option.defaultValue New)
    |> Buttons.create
        { Name = name |> Option.defaultValue "Выберите услугу"
          Columns = 1
          Data = data |> Map.ofSeq }

let internal getService (embassy: EmbassyNode) (service: ServiceNode) =
    fun (deps: Russian.Dependencies) ->
        match service.Id.Value with
        | "SRV.RU.0.1" -> service.Name |> NotSupported |> Error |> async.Return
        | _ -> deps |> Kdmid.createInstruction embassy service |> Ok |> async.Return

let internal setService (serviceId, embassy, payload) =
    fun (deps: Russian.Dependencies) ->
        deps.ServiceGraph
        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"ServiceId {serviceId.Value}" |> NotFound |> Error)
        |> ResultAsync.bindAsync (fun node ->
            match node.Children with
            | [] -> deps |> Post.pickService node embassy payload
            | _ -> node.FullName |> NotSupported |> Error |> async.Return)

let consume request =
    fun (deps: Core.Dependencies) ->
        Russian.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Request.Post postRequest ->
                match postRequest with
                | Kdmid model -> deps |> Kdmid.post model
                | Midpass model -> deps |> Midpass.post model)
