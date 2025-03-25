[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Embassies.Russian.Russian

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { Chat: Chat
      MessageId: int
      CancellationToken: CancellationToken
      Culture: Culture.Dependencies
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      sendMessageRes: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendMessagesRes: Async<Result<Producer.Message seq, Error'>> -> Async<Result<unit, Error'>>
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>> }

    static member create chat (deps: Request.Dependencies) =
        let result = ResultBuilder()

        result {
            let getChatRequests () =
                deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions

            return
                { Chat = chat
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  Culture = deps.Culture
                  ChatStorage = deps.ChatStorage
                  RequestStorage = deps.RequestStorage
                  sendMessageRes = deps.sendMessageRes
                  sendMessagesRes = deps.sendMessagesRes
                  getEmbassyGraph = deps.getEmbassyGraph
                  getServiceGraph = deps.getServiceGraph
                  getChatRequests = getChatRequests }
        }
