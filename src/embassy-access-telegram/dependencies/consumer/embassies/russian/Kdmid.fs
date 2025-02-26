[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Russian.Kdmid

open System.Threading
open EA.Telegram.Domain
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Consumer.Embassies

type Dependencies =
    { Chat: Chat
      MessageId: int
      CancellationToken: CancellationToken
      RequestStorage: Request.RequestStorage
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendResults: Async<Result<Producer.Message list, Error'>> -> Async<Result<unit, Error'>>
      getService: Graph.NodeId -> Async<Result<ServiceNode, Error'>>
      getEmbassy: Graph.NodeId -> Async<Result<EmbassyNode, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>>
      getRequest: RequestId -> Async<Result<Request, Error'>>
      createRequest: Request -> Async<Result<Request, Error'>>
      deleteRequest: RequestId -> Async<Result<unit, Error'>> }

    static member create(deps: Russian.Dependencies) =
        let result = ResultBuilder()

        result {

            let getService serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Service '%s{serviceId.Value}' for user chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some serviceNode -> serviceNode.Value |> Ok)

            let getEmbassy embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Embassy '%s{embassyId.Value}' for user chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some embassyNode -> embassyNode.Value |> Ok)

            let getRequest requestId =
                deps.RequestStorage
                |> Request.Query.tryFindById requestId
                |> ResultAsync.bind (function
                    | None ->
                        $"Request '%s{requestId.ValueStr}' for user chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some request -> request |> Ok)

            let createRequest (request: Request) =
                deps.ChatStorage
                |> Chat.Command.createChatSubscription deps.Chat.Id request.Id
                |> ResultAsync.bindAsync (fun _ -> deps.RequestStorage |> Request.Command.create request)

            let deleteRequest requestId =
                deps.ChatStorage
                |> Chat.Command.deleteChatSubscription deps.Chat.Id requestId
                |> ResultAsync.bindAsync (fun _ -> deps.RequestStorage |> Request.Command.delete requestId)

            return
                { Chat = deps.Chat
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  RequestStorage = deps.RequestStorage
                  sendResult = deps.sendResult
                  sendResults = deps.sendResults
                  getRequest = getRequest
                  getService = getService
                  getEmbassy = getEmbassy
                  getChatRequests = deps.getChatRequests
                  createRequest = createRequest
                  deleteRequest = deleteRequest }
        }
