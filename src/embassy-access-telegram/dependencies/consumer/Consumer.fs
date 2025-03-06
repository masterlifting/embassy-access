[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Consumer

open System.Threading
open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendResults: Async<Result<Producer.Message list, Error'>> -> Async<Result<unit, Error'>>
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>> }

    static member create client (payload: Consumer.Payload<_>) ct (deps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {

            let! chatStorage = deps.initChatStorage ()
            let! requestStorage = deps.initRequestStorage ()

            let getServiceGraph () =
                deps.initServiceGraphStorage () |> ResultAsync.wrap ServiceGraph.get

            let getEmbassyGraph () =
                deps.initEmbassyGraphStorage () |> ResultAsync.wrap EmbassyGraph.get

            let sendResult data =
                client
                |> Web.Telegram.Producer.produceResult data payload.ChatId ct
                |> ResultAsync.map ignore

            let sendResults data =
                client
                |> Web.Telegram.Producer.produceResultSeq data payload.ChatId ct
                |> ResultAsync.map ignore

            return
                { CancellationToken = ct
                  ChatId = payload.ChatId
                  MessageId = payload.MessageId
                  ChatStorage = chatStorage
                  RequestStorage = requestStorage
                  sendResult = sendResult
                  sendResults = sendResults
                  getServiceGraph = getServiceGraph
                  getEmbassyGraph = getEmbassyGraph }
        }
