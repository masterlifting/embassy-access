[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Producer

open Infrastructure.Domain
open Infrastructure.Prelude

type Dependencies =
    { initChatStorage: unit -> Result<EA.Telegram.DataAccess.Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<EA.Core.DataAccess.Request.RequestStorage, Error'> }

    static member create(persistenceDeps: Persistence.Dependencies) =
        let result = ResultBuilder()

        result {
            return
                { initChatStorage = persistenceDeps.initChatStorage
                  initRequestStorage = persistenceDeps.initRequestStorage }
        }
