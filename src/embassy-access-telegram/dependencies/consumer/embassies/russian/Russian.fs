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
      CultureDeps: Culture.Dependencies
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendResults: Async<Result<Producer.Message list, Error'>> -> Async<Result<unit, Error'>>
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>> }

    static member create chat (deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {
            let! cultureDeps = Culture.Dependencies.create deps

            let getChatRequests () =
                deps.RequestStorage |> Request.Query.findManyByIds chat.Subscriptions

            return
                { Chat = chat
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  CultureDeps = cultureDeps
                  ChatStorage = deps.ChatStorage
                  RequestStorage = deps.RequestStorage
                  sendResult = deps.sendResult
                  sendResults = deps.sendResults
                  getEmbassyGraph = deps.getEmbassyGraph
                  getServiceGraph = deps.getServiceGraph
                  getChatRequests = getChatRequests }
        }
