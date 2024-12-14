[<RequireQualifiedAccess>]
module EA.Telegram.Handlers.Comsumer.Russian

open System
open EA.Core.Domain
open EA.Embassies.Russian.Kdmid.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram
open EA.Telegram.Dependencies.Consumer

module private SetService =

    module private MidpassService =

        let pickService (node: Graph.Node<ServiceNode>) =
            fun deps -> node.ShortName |> NotSupported |> Error |> async.Return

    module private KdmidService =
        open EA.Embassies.Russian.Domain
        open EA.Embassies.Russian.Kdmid.Dependencies

        let inline createOrUpdateChat (request: Request) =
            fun (deps: Russian.Dependencies) ->
                deps.createOrUpdateChat
                    { Id = deps.ChatId
                      Subscriptions = [ request.Id ] |> Set }

        let getService serviceName embassy payload =
            fun (deps: Russian.Dependencies) ->
                payload
                |> Web.Http.Route.toUri
                |> ResultAsync.wrap (fun uri ->
                    let request: KdmidRequest =
                        { Uri = uri
                          Embassy = embassy
                          TimeZone = 1.0
                          Confirmation = Disabled }

                    let orderDepsRes =
                        deps.initRequestStorage ()
                        |> Result.map (fun requestStorage ->
                            Order.Dependencies.create requestStorage deps.CancellationToken)

                    orderDepsRes
                    |> Result.map (fun orderDeps ->
                        { Request = request
                          Dependencies = orderDeps }
                        |> Kdmid)
                    |> ResultAsync.wrap (EA.Embassies.Russian.API.Service.get serviceName))

        let inline createOrUpdateRequest serviceName embassy payload =
            fun (deps: Russian.Dependencies) ->
                payload
                |> Web.Http.Route.toUri
                |> ResultAsync.wrap (fun uri ->
                    let serviceRequest: KdmidRequest =
                        { Uri = uri
                          Embassy = embassy
                          TimeZone = 1.0
                          Confirmation = Disabled }

                    serviceName |> serviceRequest.CreateRequest |> deps.createOrUpdateRequest)

        let pickService (node: Graph.Node<ServiceNode>) = fun deps -> "" |> Ok |> async.Return

    let pickService (node: Graph.Node<ServiceNode>) =
        fun deps ->

            let nodeId =
                match node.FullId with
                | Graph.NodeIdValue value -> value |> Graph.splitNodeName

            match nodeId.Length with
            | length when length > 2 ->
                match nodeId[0] with
                | "RU" ->
                    match nodeId[1], nodeId[2] with
                    | "PAS", "CHK" -> deps |> MidpassService.pickService node
                    | _ -> deps |> KdmidService.pickService node
                | _ -> node.ShortName |> NotSupported |> Error |> async.Return
            | _ -> node.ShortName |> NotSupported |> Error |> async.Return

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
        |> ResultAsync.bind (fun graph ->
            match serviceIdOpt with
            | None -> graph.Children |> createButtons graph.Value.Description |> Ok
            | Some serviceId ->
                graph
                |> Graph.BFS.tryFindById serviceId
                |> Option.map Ok
                |> Option.defaultValue ("Не могу найти выбранную услугу" |> NotFound |> Error)
                |> Result.map (fun node ->
                    match node.Children with
                    | [] ->

                        let command =
                            (embassyId, serviceId, "{вставить сюда}") |> Command.SetService |> Command.set

                        let doubleLine = Environment.NewLine + Environment.NewLine
                        let message = $"%s{command}%s{doubleLine}"

                        node.Value.Instruction
                        |> Option.map (fun instruction -> message + $"Инструкция:%s{doubleLine}%s{instruction}")
                        |> Option.defaultValue message
                        |> fun msg -> ((deps.ChatId, deps.MessageId |> Replace) |> Text.create msg)
                    | services -> services |> createButtons node.Value.Description))

let internal setService (serviceId, embassy, payload) =
    fun (deps: Russian.Dependencies) ->
        deps.ServiceGraph
        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
        |> ResultAsync.bind (function
            | Some node -> Ok node
            | None -> $"ServiceId {serviceId.Value}" |> NotFound |> Error)
        |> ResultAsync.bindAsync (fun node ->
            match node.Children with
            | [] ->
                deps
                |> SetService.pickService node
                |> ResultAsync.map Text.create
                |> ResultAsync.map (fun create -> create (deps.ChatId, New))
            | _ -> node.FullName |> NotSupported |> Error |> async.Return)
