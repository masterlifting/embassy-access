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
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendResults: Async<Result<Producer.Message list, Error'>> -> Async<Result<unit, Error'>>
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>> }

    static member create client (dto: Consumer.Dto<_>) ct (deps: Persistence.Dependencies) =
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
                |> Web.Telegram.Producer.produceResult data dto.ChatId ct
                |> ResultAsync.map ignore

            let sendResults data =
                client
                |> Web.Telegram.Producer.produceResultSeq data dto.ChatId ct
                |> ResultAsync.map ignore

            return
                { CancellationToken = ct
                  ChatId = dto.ChatId
                  MessageId = dto.Id
                  sendResult = sendResult
                  sendResults = sendResults
                  ChatStorage = chatStorage
                  RequestStorage = requestStorage
                  getServiceGraph = getServiceGraph
                  getEmbassyGraph = getEmbassyGraph }
        }
