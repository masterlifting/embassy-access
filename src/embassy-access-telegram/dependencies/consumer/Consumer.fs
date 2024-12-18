﻿[<RequireQualifiedAccess>]
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
      getEmbassyGraph: unit -> Async<Result<Graph.Node<EmbassyNode>, Error'>>
      getChatRequests: unit -> Async<Result<Request list, Error'>> }

    static member create (dto: Consumer.Dto<_>) ct (persistenceDeps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {

            let! chatStorage = persistenceDeps.initChatStorage ()
            let! requestStorage = persistenceDeps.initRequestStorage ()

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

            return
                { ChatId = dto.ChatId
                  MessageId = dto.Id
                  CancellationToken = ct
                  chatStorage = chatStorage
                  requestStorage = requestStorage
                  initServiceGraphStorage = persistenceDeps.initServiceGraphStorage
                  getEmbassyGraph = persistenceDeps.getEmbassyGraph
                  getChatRequests = getChatRequests }
        }
