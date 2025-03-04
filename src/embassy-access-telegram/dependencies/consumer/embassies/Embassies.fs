[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Embassies

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies.Russian

type Dependencies =
    { Chat: Chat
      MessageId: int
      RussianDeps: Russian.Dependencies
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      getServiceNode: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getEmbassyNode: Graph.NodeId -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getEmbassiesGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getEmbassyServiceGraph: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>> }

    static member create chat (translate, translateSeq) (deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let! russianDeps = Russian.Dependencies.create chat (translate, translateSeq) deps

            let getEmbassyNode embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | Some embassy -> Ok embassy
                    | None -> $"Embassy '%s{embassyId.Value}" |> NotFound |> Error)

            let getEmbassyServiceGraph embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"Services of Embassy '%s{embassyId.Value}'"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some node ->
                        // try to get the countryId from the embassyId. It should be the second part of the embassyId
                        match node.Id.TryGetPart 1 with
                        | None ->
                            $"Services of Embassy '%s{embassyId.Value}' for user chat '%s{deps.ChatId.ValueStr}'"
                            |> NotFound
                            |> Error
                            |> async.Return
                        | Some countryId ->
                            let serviceId =
                                [ (Constants.SERVICE_NODE_ID |> Graph.NodeIdValue); countryId ]
                                |> Graph.Node.Id.combine

                            deps.getServiceGraph ()
                            |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                            |> ResultAsync.bind (function
                                | None ->
                                    $"Services of Embassy '%s{embassyId.Value}' for user chat '%s{deps.ChatId.ValueStr}'"
                                    |> NotFound
                                    |> Error
                                | Some serviceNode -> serviceNode |> Ok))

            let getServiceNode serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Service '%s{serviceId.Value}' for user chat '%s{deps.ChatId.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some serviceNode -> serviceNode |> Ok)

            return
                { Chat = chat
                  MessageId = deps.MessageId
                  RussianDeps = russianDeps
                  sendResult = deps.sendResult
                  getEmbassiesGraph = deps.getEmbassyGraph
                  getEmbassyNode = getEmbassyNode
                  getServiceNode = getServiceNode
                  getEmbassyServiceGraph = getEmbassyServiceGraph }
        }
