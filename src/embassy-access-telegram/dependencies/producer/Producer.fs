[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer.Core

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.DataAccess
open EA.Core.DataAccess
open EA.Telegram.Dependencies

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      RequestStorage: Request.RequestStorage }

    static member create chatId (persistenceDeps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {
            
            let! requestStorage = persistenceDeps.initRequestStorage()
            
            return
                { initChatStorage = persistenceDeps.initChatStorage
                  RequestStorage = requestStorage }
        }
