[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Users

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
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

    //TODO: Optimize the requests
    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let! embassiesDeps = Embassies.Dependencies.create deps

            let getUserRequests () =
                deps.ChatStorage
                |> Chat.Query.tryFindById deps.ChatId
                |> ResultAsync.bindAsync (function
                    | None -> $"User '{deps.ChatId.ValueStr}'" |> NotFound |> Error |> async.Return
                    | Some chat -> deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions)

            let getUserEmbassies () =
                getUserRequests ()
                |> ResultAsync.map (List.map _.Service.Embassy.Id)
                |> ResultAsync.bindAsync (fun embassyIds ->
                    deps.getEmbassyGraph ()
                    |> ResultAsync.map (fun node ->
                        node.Value.Description,
                        node.Children
                        |> List.filter (fun embassy ->
                            embassyIds
                            |> List.exists (fun embassyId -> embassyId.Value.Contains embassy.Id.Value))
                        |> List.map _.Value))

            let getUserEmbassyChildren embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"User embassy of Embassy with Id '{embassyId.Value}'"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some node ->
                        getUserRequests ()
                        |> ResultAsync.map (List.map _.Service.Embassy.Id)
                        |> ResultAsync.map (fun embassyIds ->
                            (node.Value.Description,
                             node.Children
                             |> List.filter (fun embassy ->
                                 embassyIds
                                 |> List.exists (fun embassyId -> embassyId.Value.Contains embassy.Id.Value))
                             |> List.map _.Value)))

            let getUserEmbassyServices (embassyId: Graph.NodeId) =
                getUserRequests ()
                |> ResultAsync.map (List.map _.Service)
                |> ResultAsync.bindAsync (fun userServices ->
                    match embassyId.TryGetPart 1 with
                    | None ->
                        $"User embassy of Embassy with Id '{embassyId.Value}'"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some countryId ->
                        let serviceId =
                            [ EA.Telegram.Domain.Constants.SERVICE_ROOT_ID |> Graph.NodeIdValue; countryId ]
                            |> Graph.Node.Id.combine

                        deps.getServiceGraph ()
                        |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                        |> ResultAsync.bind (function
                            | None -> "Service graph" |> NotFound |> Error
                            | Some node -> node |> Ok)
                        |> ResultAsync.map (fun node ->
                            node.Value.Description,
                            node.Children
                            |> List.filter (fun service ->
                                userServices
                                |> List.exists (fun userService ->
                                    userService.Id.Value.Contains service.Id.Value
                                    && userService.Embassy.Id.Value.Contains embassyId.Value))
                            |> List.map _.Value))

            let getUserEmbassyServiceChildren (embassyId: Graph.NodeId) (serviceId: Graph.NodeId) =
                getUserRequests ()
                |> ResultAsync.map (List.map _.Service)
                |> ResultAsync.bindAsync (fun userServices ->
                    deps.getServiceGraph ()
                    |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                    |> ResultAsync.bind (function
                        | None -> $"User service of Service with Id {serviceId.Value}" |> NotFound |> Error
                        | Some node ->
                            (node.Value.Description,
                             node.Children
                             |> List.filter (fun service ->
                                 userServices
                                 |> List.exists (fun userService ->
                                     userService.Id.Value.Contains service.Id.Value
                                     && userService.Embassy.Id.Value.Contains embassyId.Value))
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
