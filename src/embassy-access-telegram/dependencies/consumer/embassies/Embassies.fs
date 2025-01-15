[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Embassies

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Consumer

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      RussianDeps: RussianEmbassy.Dependencies
      getEmbassies: unit -> Async<Result<EmbassyNode list, Error'>>
      getEmbassyNode: Graph.NodeId -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceNode: Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getEmbassyServices: Graph.NodeId -> Async<Result<ServiceNode list, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let! russianDeps = RussianEmbassy.Dependencies.create deps

            let getEmbassies () =
                deps.getEmbassyGraph () |> ResultAsync.map (_.Children >> List.map _.Value)

            let getEmbassyNode embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | Some embassy -> Ok embassy
                    | None -> $"Embassy with Id {embassyId.Value}" |> NotFound |> Error)

            let getEmbassyServices embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"Embassy services of Embassy with Id {embassyId.Value}"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some embassyNode ->
                        match embassyNode.IdParts.Length > 1 with
                        | false ->
                            $"Embassy services of {embassyNode.ShortName}"
                            |> NotFound
                            |> Error
                            |> async.Return
                        | true ->
                            let serviceId =
                                [ "SRV"; embassyNode.IdParts[1].Value ] |> Graph.combine |> Graph.NodeIdValue

                            deps.getServiceGraph ()
                            |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                            |> ResultAsync.bind (function
                                | None -> $"Embassy services of {embassyNode.ShortName}" |> NotFound |> Error
                                | Some serviceNode -> serviceNode.Children |> List.map _.Value |> Ok))

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
                  getEmbassies = getEmbassies
                  getEmbassyNode = getEmbassyNode
                  getServiceNode = getServiceNode
                  getEmbassyServices = getEmbassyServices }
        }
