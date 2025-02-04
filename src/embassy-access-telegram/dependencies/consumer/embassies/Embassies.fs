[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Embassies

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies.Russian

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      RussianDeps: Russian.Dependencies
      sendResult: Async<Result<Producer.Data, Error'>> -> Async<Result<unit, Error'>>
      getServiceNode: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getEmbassyNode: Graph.NodeId -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getEmbassyServiceGraph: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let! russianDeps = Russian.Dependencies.create deps

            let getEmbassyNode embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | Some embassy -> Ok embassy
                    | None -> $"Embassy with Id {embassyId.Value}" |> NotFound |> Error)

            let getEmbassyServiceGraph embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"Embassy services of Embassy with Id {embassyId.Value}"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some embassyNode ->
                        match embassyNode.Id.TryGetPart 1 with
                        | None ->
                            $"Embassy services of {embassyNode.ShortName}"
                            |> NotFound
                            |> Error
                            |> async.Return
                        | Some countryId ->
                            let serviceId =
                                [ (EA.Telegram.Domain.Constants.SERVICE_ROOT_ID |> Graph.NodeIdValue)
                                  countryId ]
                                |> Graph.Node.Id.combine

                            deps.getServiceGraph ()
                            |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                            |> ResultAsync.bind (function
                                | None -> $"Embassy services of {embassyNode.ShortName}" |> NotFound |> Error
                                | Some serviceNode -> serviceNode |> Ok))

            let getServiceNode serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                |> ResultAsync.bind (function
                    | None -> $"Service with Id {serviceId.Value}" |> NotFound |> Error
                    | Some serviceNode -> serviceNode |> Ok)

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  RussianDeps = russianDeps
                  sendResult = deps.sendResult
                  getEmbassiesGraph = deps.getEmbassyGraph
                  getEmbassyNode = getEmbassyNode
                  getServiceNode = getServiceNode
                  getEmbassyServiceGraph = getEmbassyServiceGraph }
        }
