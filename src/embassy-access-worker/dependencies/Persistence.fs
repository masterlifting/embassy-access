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

            let! connectionString = cfg |> Persistence.Storage.getConnectionString "FileSystem"

            let initChatStorage () =
                connectionString |> Chat.FileSystem |> Chat.init

            let initRequestStorage () =
                connectionString |> Request.FileSystem |> Request.init

            return
                { initChatStorage = initChatStorage
                  initRequestStorage = initRequestStorage }
        }
