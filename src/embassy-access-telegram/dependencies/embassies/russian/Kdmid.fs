[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Kdmid

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Producer
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies
open EA.Embassies.Russian
open EA.Embassies.Russian.Domain
open EA.Embassies.Russian.Kdmid.Dependencies

module Notification =

    type Dependencies =
        { getRequestChats: Request -> Async<Result<Chat list, Error'>>
          setRequestAppointments: Graph.NodeId -> Appointment Set -> Async<Result<Request list, Error'>>
          translateMessages: Culture -> Message seq -> Async<Result<Message list, Error'>>
          sendMessages: Message seq -> Async<Result<unit, Error'>> }

        static member create() =
            fun (deps: Russian.Dependencies) ->
                { translateMessages = deps.Culture.translateSeq
                  setRequestAppointments = deps.setRequestAppointments
                  getRequestChats = deps.getRequestChats
                  sendMessages = deps.sendMessages }

type Dependencies =
    { Chat: Chat
      MessageId: int
      CancellationToken: CancellationToken
      RequestStorage: Request.RequestStorage
      sendMessageRes: Async<Result<Message, Error'>> -> Async<Result<unit, Error'>>
      sendMessagesRes: Async<Result<Message seq, Error'>> -> Async<Result<unit, Error'>>
      getService: Graph.NodeId -> Async<Result<ServiceNode, Error'>>
      getEmbassy: Graph.NodeId -> Async<Result<EmbassyNode, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>>
      getRequest: RequestId -> Async<Result<Request, Error'>>
      createRequest: Request -> Async<Result<Request, Error'>>
      deleteRequest: RequestId -> Async<Result<unit, Error'>>
      getApi: Request -> Async<Result<Request, Error'>>
      translateMessageRes: Async<Result<Message, Error'>> -> Async<Result<Message, Error'>>
      translateMessagesRes: Async<Result<Message list, Error'>> -> Async<Result<Message seq, Error'>> }

    static member create(deps: Russian.Dependencies) =
        let result = ResultBuilder()

        result {

            let getService serviceId =
                deps.getServiceGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById serviceId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Service '%s{serviceId.Value}' for the chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some serviceNode -> serviceNode.Value |> Ok)

            let getEmbassy embassyId =
                deps.getEmbassyGraph ()
                |> ResultAsync.map (Graph.BFS.tryFindById embassyId)
                |> ResultAsync.bind (function
                    | None ->
                        $"Embassy '%s{embassyId.Value}' for the chat '%s{deps.Chat.Id.ValueStr}'"
                        |> NotFound
                        |> Error
                    | Some embassyNode -> embassyNode.Value |> Ok)

            let getRequest requestId =
                deps.RequestStorage
                |> Request.Query.tryFindById requestId
                |> ResultAsync.bind (function
                    | None ->
                        $"Request '%s{requestId.ValueStr}' for the chat '%s{deps.Chat.Id.ValueStr}'"
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

            let apiDeps = Order.Dependencies.create deps.RequestStorage deps.CancellationToken

            let getApi request =
                { Request = request
                  Dependencies = apiDeps }
                |> Kdmid
                |> API.Service.get

            let translateMessageRes = deps.Culture.translateRes deps.Chat.Culture

            let translateMessagesRes =
                ResultAsync.map Seq.ofList
                >> deps.Culture.translateSeqRes deps.Chat.Culture
                >> ResultAsync.map Seq.ofList

            return
                { Chat = deps.Chat
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  RequestStorage = deps.RequestStorage
                  sendMessageRes = deps.sendMessageRes
                  sendMessagesRes = deps.sendMessagesRes
                  getRequest = getRequest
                  getService = getService
                  getEmbassy = getEmbassy
                  getChatRequests = deps.getChatRequests
                  createRequest = createRequest
                  deleteRequest = deleteRequest
                  getApi = getApi
                  translateMessageRes = translateMessageRes
                  translateMessagesRes = translateMessagesRes }
        }
