[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Configuration
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Core.DataAccess

type Dependencies =
    { initChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! filePath = cfg |> Persistence.Storage.getConnectionString "FileSystem"

            let initializeChatStorage () =
                filePath |> Chat.FileSystem |> Chat.init

            let initializeRequestStorage () =
                filePath |> Request.FileSystem |> Request.init

            return
                { initChatStorage = initializeChatStorage
                  initRequestStorage = initializeRequestStorage }
        }
