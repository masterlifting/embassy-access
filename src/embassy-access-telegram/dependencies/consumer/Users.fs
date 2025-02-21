[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Users

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Consumer.Embassies

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      EmbassiesDeps: Embassies.Dependencies
      sendResult: Async<Result<Producer.Data, Error'>> -> Async<Result<unit, Error'>>
      getUserEmbassies: unit -> Async<Result<string option * EmbassyNode list, Error'>>
      getUserEmbassyChildren: Graph.NodeId -> Async<Result<string option * EmbassyNode list, Error'>>
      getUserEmbassyServices: Graph.NodeId -> Async<Result<string option * ServiceNode list, Error'>>
      getUserEmbassyServiceChildren:
          Graph.NodeId -> Graph.NodeId -> Async<Result<string option * ServiceNode list, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let! embassiesDeps = Embassies.Dependencies.create deps

            let getUserRequests () =
                deps.ChatStorage
                |> Chat.Query.tryFindById deps.ChatId
                |> ResultAsync.bindAsync (function
                    | None -> $"User chat '%s{deps.ChatId.ValueStr}'" |> NotFound |> Error |> async.Return
                    | Some chat -> deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions)

            let getUserEmbassies () =
                getUserRequests ()
                |> ResultAsync.map (List.map _.Service.Embassy.Id >> List.distinct)
                |> ResultAsync.bindAsync (fun embassyIds ->
                    deps.getEmbassyGraph ()
                    |> ResultAsync.map (fun node ->
                        node.Value.Description,
                        node.Children
                        |> List.filter (fun embassy -> embassy.Id.In embassyIds)
                        |> List.map _.Value))

            let getUserEmbassyChildren embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"Embassy '%s{embassyId.Value}' for user chat '%s{deps.ChatId.ValueStr}'"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some node ->
                        getUserRequests ()
                        |> ResultAsync.map (List.map _.Service.Embassy.Id >> List.distinct)
                        |> ResultAsync.map (fun embassyIds ->
                            node.Value.Description,
                            node.Children
                            |> List.filter (fun embassy -> embassy.Id.In embassyIds)
                            |> List.map _.Value))

            let getUserEmbassyServices (embassyId: Graph.NodeId) =
                getUserRequests ()
                |> ResultAsync.map (List.map _.Service)
                |> ResultAsync.bindAsync (fun userServices ->
                    // try to get the countryId from the embassyId. It should be the second part of the embassyId
                    match embassyId.TryGetPart 1 with
                    | None ->
                        $"Services of Embassy '%s{embassyId.Value}' for user chat '%s{deps.ChatId.ValueStr}'"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some countryId ->
                        let serviceId =
                            [ Constants.SERVICE_NODE_ID |> Graph.NodeIdValue; countryId ]
                            |> Graph.Node.Id.combine

                        deps.getServiceGraph ()
                        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                        |> ResultAsync.bind (function
                            | None ->
                                $"Service '%s{serviceId.Value}' of Embassy '%s{embassyId.Value}' for user chat '%s{deps.ChatId.ValueStr}'"
                                |> NotFound
                                |> Error
                            | Some node ->
                                (node.Value.Description,
                                 node.Children
                                 |> List.filter (fun service ->
                                     userServices
                                     |> List.exists (fun userService ->
                                         userService.Id.Contains service.Id
                                         && userService.Embassy.Id.Contains embassyId))
                                 |> List.map _.Value)
                                |> Ok))

            let getUserEmbassyServiceChildren (embassyId: Graph.NodeId) (serviceId: Graph.NodeId) =
                getUserRequests ()
                |> ResultAsync.map (List.map _.Service)
                |> ResultAsync.bindAsync (fun userServices ->
                    deps.getServiceGraph ()
                    |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                    |> ResultAsync.bind (function
                        | None ->
                            $"Service '%s{serviceId.Value}' of Embassy '%s{embassyId.Value}' for user chat '%s{deps.ChatId.ValueStr}'"
                            |> NotFound
                            |> Error
                        | Some node ->
                            (node.Value.Description,
                             node.Children
                             |> List.filter (fun service ->
                                 userServices
                                 |> List.exists (fun userService ->
                                     userService.Id.Contains service.Id && userService.Embassy.Id.Contains embassyId))
                             |> List.map _.Value)
                            |> Ok))

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  EmbassiesDeps = embassiesDeps
                  sendResult = deps.sendResult
                  getUserEmbassies = getUserEmbassies
                  getUserEmbassyServices = getUserEmbassyServices
                  getUserEmbassyChildren = getUserEmbassyChildren
                  getUserEmbassyServiceChildren = getUserEmbassyServiceChildren }
        }
