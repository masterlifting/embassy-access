[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Request

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
      Culture: Culture.Dependencies
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      sendResult: Async<Result<Producer.Message, Error'>> -> Async<Result<unit, Error'>>
      sendResults: Async<Result<Producer.Message list, Error'>> -> Async<Result<unit, Error'>> }

    static member create(payload: Consumer.Payload<_>) =
        fun (deps: Consumer.Dependencies) ->
            let result = ResultBuilder()

            result {

                let! chatStorage = deps.initChatStorage ()
                let! requestStorage = deps.initRequestStorage ()
                let! aiProvider = deps.initAIProvider ()
                let! cultureStorage = deps.initCultureStorage ()

                let! cultureDeps =
                    Culture.Dependencies.create
                        payload.ChatId
                        payload.MessageId
                        deps.CancellationToken
                        aiProvider
                        chatStorage
                        cultureStorage
                        deps.CulturePlaceholder

                let getServiceGraph () =
                    deps.initServiceGraphStorage () |> ResultAsync.wrap ServiceGraph.get

                let getEmbassyGraph () =
                    deps.initEmbassyGraphStorage () |> ResultAsync.wrap EmbassyGraph.get

                let sendResult data =
                    deps.TelegramClient
                    |> Web.Telegram.Producer.produceResult data payload.ChatId deps.CancellationToken
                    |> ResultAsync.map ignore

                let sendResults data =
                    deps.TelegramClient
                    |> Web.Telegram.Producer.produceResultSeq data payload.ChatId deps.CancellationToken
                    |> ResultAsync.map ignore

                return
                    { ChatId = payload.ChatId
                      MessageId = payload.MessageId
                      CancellationToken = deps.CancellationToken
                      Culture = cultureDeps
                      ChatStorage = chatStorage
                      RequestStorage = requestStorage
                      getServiceGraph = getServiceGraph
                      getEmbassyGraph = getEmbassyGraph
                      sendResult = sendResult
                      sendResults = sendResults }
            }
