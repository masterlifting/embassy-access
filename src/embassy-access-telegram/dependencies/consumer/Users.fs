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
      getUserEmbassies: ChatId -> Async<Result<string option * EmbassyNode list, Error'>>
      getUserEmbassyChildren: ChatId -> Graph.NodeId -> Async<Result<string option * EmbassyNode list, Error'>>
      getUserEmbassyServices: ChatId -> Graph.NodeId -> Async<Result<string option * ServiceNode list, Error'>>
      getUserEmbassyServiceChildren:
          ChatId -> Graph.NodeId -> Graph.NodeId -> Async<Result<string option * ServiceNode list, Error'>> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let! embassiesDeps = Embassies.Dependencies.create deps

            let getUserRequests chatId =
                deps.ChatStorage
                |> Chat.Query.tryFindById chatId
                |> ResultAsync.bindAsync (function
                    | None -> $"Telegram chat {chatId.ValueStr}" |> NotFound |> Error |> async.Return
                    | Some chat -> deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions)

            let getUserEmbassies chatId =
                getUserRequests chatId
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

            let getUserEmbassyChildren chatId embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bindAsync (function
                    | None ->
                        $"User embassy of Embassy with Id {embassyId.Value}"
                        |> NotFound
                        |> Error
                        |> async.Return
                    | Some node ->
                        getUserRequests chatId
                        |> ResultAsync.map (List.map _.Service.Embassy.Id)
                        |> ResultAsync.map (fun embassyIds ->
                            (node.Value.Description,
                             node.Children
                             |> List.filter (fun embassy ->
                                 embassyIds
                                 |> List.exists (fun embassyId -> embassyId.Value.Contains embassy.Id.Value))
                             |> List.map _.Value)))

            let getUserEmbassyServices chatId (embassyId: Graph.NodeId) =
                getUserRequests chatId
                |> ResultAsync.map (List.map _.Service)
                |> ResultAsync.bindAsync (fun userServices ->
                    let embassyIdSubdomain = embassyId.Value |> Graph.split |> Seq.take 2 |> Seq.last

                    deps.getServiceGraph ()
                    |> ResultAsync.map (Graph.BFS.tryFindById ($"SRV.{embassyIdSubdomain}" |> Graph.NodeIdValue))
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

            let getUserEmbassyServiceChildren chatId embassyId (serviceId: Graph.NodeId) =
                getUserEmbassyServices chatId embassyId
                |> ResultAsync.map (fun (parentDescription, userEmbassyServices) ->
                    parentDescription,
                    userEmbassyServices
                    |> List.filter (fun userEmbassyService -> userEmbassyService.Id.Value.Contains serviceId.Value))

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  EmbassiesDeps = embassiesDeps
                  getUserEmbassies = getUserEmbassies
                  getUserEmbassyServices = getUserEmbassyServices
                  getUserEmbassyChildren = getUserEmbassyChildren
                  getUserEmbassyServiceChildren = getUserEmbassyServiceChildren }
        }
