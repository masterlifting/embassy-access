[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Core

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.DataAccess
open EA.Core.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }

    static member create chatId (persistenceDeps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {
            return
                { initChatStorage = persistenceDeps.initChatStorage
                  initRequestStorage = persistenceDeps.initRequestStorage }
        }
