[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.CommandHandler.Core

open System.Threading
open EA.Telegram.DataAccess
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Web.Telegram.Domain
open EA.Core.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { ChatId: ChatId
      MessageId: int
      CancellationToken: CancellationToken
      initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'>
      initServiceGraphStorage: string -> Result<ServiceGraph.ServiceGraphStorage, Error'> }

    static member create(deps: Consumer.Dependencies) =
        let result = ResultBuilder()

        result {
            return
                { ChatId = deps.ChatId
                  MessageId = deps.MessageId
                  CancellationToken = deps.CancellationToken
                  initChatStorage = deps.Persistence.initChatStorage
                  initRequestStorage = deps.Persistence.initRequestStorage
                  initServiceGraphStorage = deps.Persistence.initServiceGraphStorage }
        }
