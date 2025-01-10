[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Russian

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      RequestStorage: Request.RequestStorage
      getServiceNode: Graph.NodeId -> Async<Result<ServiceNode, Error'>>
      getEmbassyNode: Graph.NodeId -> Async<Result<EmbassyNode, Error'>>
      getEmbassyRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
      getRequest: RequestId -> Async<Result<Request, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>>
      createChatSubscription: RequestId -> Async<Result<unit, Error'>>
      createRequest: Request -> Async<Result<Request, Error'>> }

    static member create(deps: EA.Telegram.Dependencies.Consumer.Core.Dependencies) =
        let result = ResultBuilder()

        result {

            let createChatSubscription subscriptionId =
                deps.ChatStorage
                |> Chat.Command.createChatSubscription deps.ChatId subscriptionId

            let createRequest request =
                deps.RequestStorage |> Request.Command.create request

            let getServices serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                |> ResultAsync.bind (function
                    | None -> $"Service with Id {serviceId.Value}" |> NotFound |> Error
                    | Some serviceNode -> serviceNode.Value |> Ok)

            let getEmbassy embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | None -> $"Embassy with Id {embassyId.Value}" |> NotFound |> Error
                    | Some embassyNode -> embassyNode.Value |> Ok)

            let getRequest requestId =
                deps.RequestStorage
                |> Request.Query.tryFindById requestId
                |> ResultAsync.bind (function
                    | None -> $"Request with Id {requestId}" |> NotFound |> Error
                    | Some request -> request |> Ok)

            let getEmbassyRequests embassyId =
                deps.RequestStorage |> Request.Query.findManyByEmbassyId embassyId

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  RequestStorage = deps.RequestStorage
                  getRequest = getRequest
                  getServiceNode = getServices
                  getEmbassyNode = getEmbassy
                  getEmbassyRequests = getEmbassyRequests
                  getChatRequests = deps.getChatRequests
                  createChatSubscription = createChatSubscription
                  createRequest = createRequest }
        }
