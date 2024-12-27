[<RequireQualifiedAccess>]
module EA.Telegram.Consumer.Dependencies.Users

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Core.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      EmbassiesDeps: Embassies.Dependencies
      getUserEmbassy: ChatId -> Graph.NodeId -> Async<Result<EmbassyNode * EmbassyNode list, Error'>>
      getUserEmbassyNodes: ChatId -> Async<Result<Graph.Node<EmbassyNode> list, Error'>>
      getUserEmbassyServiceNode:
          ChatId -> Graph.NodeId -> Graph.NodeId -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getUserEmbassyServiceNodes: ChatId -> Graph.NodeId -> Async<Result<Graph.Node<ServiceNode> list, Error'>> }

    static member create(deps: Core.Dependencies) =
        let result = ResultBuilder()

        result {

            let! embassiesDeps = Embassies.Dependencies.create deps

            let getUserServices chatId =
                deps.ChatStorage
                |> Chat.Query.tryFindById chatId
                |> ResultAsync.bindAsync (function
                    | None -> $"Telegram chat {chatId.ValueStr}" |> NotFound |> Error |> async.Return
                    | Some chat ->
                        deps.RequestStorage
                        |> Request.Query.findManyByIds chat.Subscriptions
                        |> ResultAsync.map (List.map _.Service))

            let getUserEmbassyNodes chatId =
                getUserServices chatId
                |> ResultAsync.map (List.map _.Embassy.Id)
                |> ResultAsync.bindAsync (fun embassyIds ->
                    deps.getEmbassyGraph ()
                    |> ResultAsync.map _.Children
                    |> ResultAsync.map (
                        List.filter (fun x -> embassyIds |> List.exists (fun y -> y.Value.Contains x.Id.Value))
                    ))

            let getUserEmbassy chatId embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"User embassy of Embassy with Id {embassyId.Value}"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some embassyNode ->
                        getUserServices chatId
                        |> ResultAsync.map (List.map _.Embassy.Id)
                        |> ResultAsync.map (fun embassyIds ->
                            (embassyNode.Value,
                             embassyNode.Children
                             |> List.filter (fun x -> embassyIds |> List.exists (fun y -> y.Value.Contains x.Id.Value))
                             |> List.map _.Value)))

            let getUserEmbassyServiceNodes chatId (embassyId: Graph.NodeId) =
                getUserServices chatId
                |> ResultAsync.bindAsync (fun services ->
                    deps.getServiceGraph ()
                    |> ResultAsync.map _.Children
                    |> ResultAsync.map (
                        List.filter (fun x ->
                            services
                            |> List.exists (fun y ->
                                y.Id.Value.Contains x.Id.Value && y.Embassy.Id.Value.Contains embassyId.Value))
                    ))

            let getUserEmbassyServiceNode chatId embassyId serviceId =
                getUserEmbassyServiceNodes chatId embassyId
                |> ResultAsync.map (List.tryFind (fun x -> x.Id = serviceId))
                |> ResultAsync.bind (function
                    | None ->
                        $"User embassy service of Embassy with Id {embassyId.Value} and Service with Id {serviceId.Value}"
                        |> NotFound
                        |> Error
                    | Some node -> node |> Ok)

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  EmbassiesDeps = embassiesDeps
                  getUserEmbassy = getUserEmbassy
                  getUserEmbassyNodes = getUserEmbassyNodes
                  getUserEmbassyServiceNode = getUserEmbassyServiceNode
                  getUserEmbassyServiceNodes = getUserEmbassyServiceNodes }
        }
