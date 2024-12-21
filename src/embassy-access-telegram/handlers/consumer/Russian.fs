[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Comsumer.Russian

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

module private SetService =
    let private createMessage request =
        let errorFilter error = true

        Notification.tryCreate errorFilter request
        |> Option.map _.Message
        |> Option.defaultValue
            $"Не удалось создать уведомление для запроса {request.Id} и результата {request.ProcessState}"

    module private MidpassService =

        let pickService (node: Graph.Node<ServiceNode>) embassy payload =
            fun deps -> node.ShortName |> NotSupported |> Error |> async.Return

    module private KdmidService =
        open EA.Embassies.Russian.Domain
        open EA.Embassies.Russian.Kdmid.Dependencies

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
                                |> Option.map (fun instruction ->
                                    message + $"Инструкция:%s{doubleLine}%s{instruction}")
                                |> Option.defaultValue message
                                |> Text.create
                                |> fun create ->
                                    (deps.ChatId, deps.MessageId |> Replace) |> create |> Ok |> async.Return)
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
                    | "1", "3" -> deps |> MidpassService.pickService node embassy payload
                    | _ -> deps |> KdmidService.pickService node embassy payload
                | _ -> node.ShortName |> NotSupported |> Error |> async.Return
            | _ -> node.ShortName |> NotSupported |> Error |> async.Return


let internal sendInstruction (service: ServiceNode) (embassy: EmbassyNode) =
    fun (deps: Russian.Dependencies) ->
        let request =
            Router.Services(
                Post(
                    { ServiceId = service.Id
                      EmbassyId = embassy.Id
                      Payload = "{вставить сюда}" }
                )
            )

        let doubleLine = Environment.NewLine + Environment.NewLine
        let message = $"%s{request.Route}%s{doubleLine}"

        service.Instruction
        |> Option.map (fun instruction -> message + $"Инструкция:%s{doubleLine}%s{instruction}")
        |> Option.defaultValue message
        |> Text.create
        |> fun create -> (deps.ChatId, deps.MessageId |> Replace) |> create

let internal getService (embassyId, serviceIdOpt) =
    fun (deps: Russian.Dependencies) ->

        let inline createButtons buttonName (nodes: Graph.Node<ServiceNode> seq) =
            nodes
            |> Seq.map (fun node -> (embassyId, node.FullId) |> Command.GetService |> Command.set, node.ShortName)
            |> Map
            |> fun buttons ->
                (deps.ChatId, deps.MessageId |> Replace)
                |> Buttons.create
                    { Name = buttonName |> Option.defaultValue "Какую услугу вы хотите получить?"
                      Columns = 1
                      Data = buttons }

        deps.ServiceGraph
        |> ResultAsync.bindAsync (fun graph ->
            match serviceIdOpt with
            | None -> graph.Children |> createButtons graph.Value.Description |> Ok |> async.Return
            | Some serviceId ->
                graph
                |> Graph.BFS.tryFindById serviceId
                |> Option.map Ok
                |> Option.defaultValue ("Не могу найти выбранную услугу" |> NotFound |> Error)
                |> ResultAsync.wrap (fun node ->
                    match node.Children with
                    | [] ->
                        deps.getChatRequests ()
                        |> ResultAsync.map (fun requests ->
                            match
                                requests
                                |> List.filter (fun request ->
                                    request.Service.Name = node.Value.Name && request.Service.Embassy.Id = embassyId)
                            with
                            | [] ->
                                let command =
                                    (embassyId, serviceId, "{вставить сюда}") |> Command.SetService |> Command.set

                                let doubleLine = Environment.NewLine + Environment.NewLine
                                let message = $"%s{command}%s{doubleLine}"

                                node.Value.Instruction
                                |> Option.map (fun instruction ->
                                    message + $"Инструкция:%s{doubleLine}%s{instruction}")
                                |> Option.defaultValue message
                                |> Text.create
                                |> fun create -> (deps.ChatId, deps.MessageId |> Replace) |> create
                            | _ ->
                                requests
                                |> Seq.map (fun request ->
                                    (embassyId, node.FullId, request.Id.ValueStr)
                                    |> Command.SetService
                                    |> Command.set,
                                    request.Service.Payload)
                                |> Seq.append
                                    [ (embassyId, node.FullId, "0") |> Command.SetService |> Command.set,
                                      "Новый запрос" ]
                                |> Map
                                |> fun buttons ->
                                    (deps.ChatId, deps.MessageId |> Replace)
                                    |> Buttons.create
                                        { Name = node.Value.Description |> Option.defaultValue "Выберите запрос"
                                          Columns = 1
                                          Data = buttons })
                    | services -> services |> createButtons node.Value.Description |> Ok |> async.Return))

let internal setService (serviceId, embassy, payload) =
    fun (deps: Russian.Dependencies) ->
        deps.ServiceGraph
        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"ServiceId {serviceId.Value}" |> NotFound |> Error)
        |> ResultAsync.bindAsync (fun node ->
            match node.Children with
            | [] -> deps |> SetService.pickService node embassy payload
            | _ -> node.FullName |> NotSupported |> Error |> async.Return)
