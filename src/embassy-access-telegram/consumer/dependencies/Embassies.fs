[<RequireQualifiedAccess>]
module EA.Telegram.Consumer.Dependencies.Embassies

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      RussianEmbassyDeps: RussianEmbassy.Dependencies
      getEmbassies: unit -> Async<Result<EmbassyNode list, Error'>>
      getEmbassyNode: Graph.NodeId -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getEmbassyServiceNodes: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode> list, Error'>>
      getEmbassyServiceNode: Graph.NodeId -> Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>> }

    static member create(deps: Core.Dependencies) =
        let result = ResultBuilder()

        result {

            let! russianServiceDeps = RussianEmbassy.Dependencies.create deps

            let getEmbassies () =
                deps.getEmbassyGraph () |> ResultAsync.map (_.Children >> List.map _.Value)

            let getEmbassyNode embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | Some embassy -> Ok embassy
                    | None -> $"Embassy with Id {embassyId.Value}" |> NotFound |> Error)

            let getEmbassyServiceNodes embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None -> $"Embassy with Id {embassyId.Value}" |> NotFound |> Error |> async.Return
                    | Some embassyNode ->
                        match embassyNode.IdParts.Length > 1 with
                        | false -> embassyNode.Name |> NotSupported |> Error |> async.Return
                        | true ->
                            let serviceId =
                                [ "SRV"; embassyNode.IdParts[1].Value ] |> Graph.combine |> Graph.NodeIdValue

                            deps.getServiceGraph ()
                            |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                            |> ResultAsync.bind (function
                                | None -> $"Service of embassy with Id {embassyId.Value}" |> NotFound |> Error
                                | Some serviceNode -> serviceNode.Children |> Ok))

            let getEmbassyServiceNode embassyId serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                |> ResultAsync.bind (function
                    | None -> $"Service with Id {serviceId.Value}" |> NotFound |> Error
                    | Some serviceNode -> serviceNode |> Ok)

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  RussianEmbassyDeps = russianServiceDeps
                  getEmbassies = getEmbassies
                  getEmbassyNode = getEmbassyNode
                  getEmbassyServiceNodes = getEmbassyServiceNodes
                  getEmbassyServiceNode = getEmbassyServiceNode }
        }
