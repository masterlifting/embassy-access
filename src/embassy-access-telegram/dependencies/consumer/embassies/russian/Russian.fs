[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Embassies.Russian.Russian

open System.Threading
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies.Consumer

type Dependencies =
    { Chat: Chat
      MessageId: int
      CancellationToken: CancellationToken
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendResults: Async<Result<Producer.Message list, Error'>> -> Async<Result<unit, Error'>>
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>> }

    static member create chat (deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {

            let getChatRequests () =
                deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions

            return
                { Chat = chat
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  sendResult = deps.sendResult
                  sendResults = deps.sendResults
                  ChatStorage = deps.ChatStorage
                  RequestStorage = deps.RequestStorage
                  getEmbassyGraph = deps.getEmbassyGraph
                  getServiceGraph = deps.getServiceGraph
                  getChatRequests = getChatRequests }
        }
