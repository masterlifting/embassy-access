[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Consumer.Core

open System.Threading
open Infrastructure.Prelude
open Infrastructure.Domain
open Web.Telegram.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      ChatStorage: Chat.ChatStorage
      RequestStorage: Request.RequestStorage
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getServiceGraph: unit -> Async<Result<Graph.Node<ServiceNode>, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>> }

    static member create (dto: Consumer.Dto<_>) ct (deps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {

            let! chatStorage = deps.initChatStorage ()
            let! requestStorage = deps.initRequestStorage ()

            let getChatRequests () =
                chatStorage
                |> Chat.Query.tryFindById dto.ChatId
                |> ResultAsync.bindAsync (function
                    | None ->
                        let chat: EA.Telegram.Domain.Chat.Chat =
                            { Id = dto.ChatId
                              Subscriptions = Set [] }

                        chatStorage |> Chat.Command.create chat |> ResultAsync.map (fun _ -> [])
                    | Some chat -> requestStorage |> Request.Query.findManyByIds chat.Subscriptions)

            let getServiceGraph () =
                deps.initServiceGraphStorage () |> ResultAsync.wrap ServiceGraph.get

            let getEmbassyGraph () =
                deps.initEmbassyGraphStorage () |> ResultAsync.wrap EmbassyGraph.get

            return
                { CancellationToken = ct
                  ChatId = dto.ChatId
                  MessageId = dto.Id
                  ChatStorage = chatStorage
                  RequestStorage = requestStorage
                  getServiceGraph = getServiceGraph
                  getEmbassyGraph = getEmbassyGraph
                  getChatRequests = getChatRequests }
        }
