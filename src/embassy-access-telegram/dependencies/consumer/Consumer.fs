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
      chatStorage: Chat.ChatStorage
      requestStorage: Request.RequestStorage
      initServiceGraphStorage: string -> Result<ServiceGraph.ServiceGraphStorage, Error'>
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyGraph>, Error'>>
      getChatEmbassies: unit -> Async<Result<EmbassyGraph list, Error'>> }

    static member create (dto: Consumer.Dto<_>) ct (persistenceDeps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {

            let! chatStorage = persistenceDeps.initChatStorage ()
            let! requestStorage = persistenceDeps.initRequestStorage ()

            let getChatEmbassies () =
                chatStorage
                |> Chat.Query.tryFindById dto.ChatId
                |> ResultAsync.bindAsync (function
                    | None -> $"{dto.ChatId}" |> NotFound |> Error |> async.Return
                    | Some chat -> requestStorage |> Request.Query.Embassy.findManyByRequestIds chat.Subscriptions)

            return
                { ChatId = dto.ChatId
                  MessageId = dto.Id
                  CancellationToken = ct
                  chatStorage = chatStorage
                  requestStorage = requestStorage
                  initServiceGraphStorage = persistenceDeps.initServiceGraphStorage
                  getEmbassyGraph = persistenceDeps.getEmbassyGraph
                  getChatEmbassies = getChatEmbassies }
        }
