[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Russian.Kdmid

open System.Threading
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
      CancellationToken: CancellationToken
      RequestStorage: Request.RequestStorage
      sendResult: Async<Result<Producer.Data, Error'>> -> Async<Result<unit, Error'>>
      sendResults: Async<Result<Producer.Data list, Error'>> -> Async<Result<unit, Error'>>
      getService: Graph.NodeId -> Async<Result<ServiceNode, Error'>>
      getEmbassy: Graph.NodeId -> Async<Result<EmbassyNode, Error'>>
      getSubscriptionsChats: RequestId seq -> Async<Result<Chat list, Error'>>
      getEmbassyRequests: Graph.NodeId -> Async<Result<Request list, Error'>>
      getRequest: RequestId -> Async<Result<Request, Error'>>
      updateRequest: Request -> Async<Result<Request, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>>
      createRequest: Request -> Async<Result<Request, Error'>> }

    static member create(deps: Russian.Dependencies) =
        let result = ResultBuilder()

        result {

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

            let updateRequest request =
                deps.RequestStorage |> Request.Command.update request

            let getEmbassyRequests embassyId =
                deps.RequestStorage |> Request.Query.findManyByEmbassyId embassyId

            let getSubscriptionsChats (requestIds: RequestId seq) =
                deps.ChatStorage |> Chat.Query.findManyBySubscriptions requestIds

            let createRequest (request: Request) =
                deps.ChatStorage
                |> Chat.Command.createChatSubscription deps.ChatId request.Id
                |> ResultAsync.bindAsync (fun _ -> deps.RequestStorage |> Request.Command.create request)

            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  RequestStorage = deps.RequestStorage
                  sendResult = deps.sendResult
                  sendResults = deps.sendResults
                  getRequest = getRequest
                  updateRequest = updateRequest
                  getService = getServices
                  getEmbassy = getEmbassy
                  getEmbassyRequests = getEmbassyRequests
                  getSubscriptionsChats = getSubscriptionsChats
                  getChatRequests = deps.getChatRequests
                  createRequest = createRequest }
        }
