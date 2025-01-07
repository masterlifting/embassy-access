[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { initTelegramChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! connectionString = cfg |> Persistence.Storage.getConnectionString "FileSystem"

            let initChatStorage () =
                connectionString |> Chat.FileSystem |> Chat.init

            let initRequestStorage () =
                connectionString |> Request.FileSystem |> Request.init

            return
                { initTelegramChatStorage = initChatStorage
                  initRequestStorage = initRequestStorage }
        }
