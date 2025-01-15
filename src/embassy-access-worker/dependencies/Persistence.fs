[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Client
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess

type Dependencies =
    { initTelegramClient: unit -> Result<TelegramBot, Error'>
      initTelegramChatStorage: unit -> Result<Chat.ChatStorage, Error'>
      initRequestStorage: unit -> Result<Request.RequestStorage, Error'> }

    static member create cfg =
        let result = ResultBuilder()

        result {

            let! connectionString = cfg |> Persistence.Storage.getConnectionString "FileSystem"

            let initTelegramClient () =
                EA.Worker.Domain.Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN_KEY
                |> EnvKey
                |> Web.Telegram.Client.init

            let initChatStorage () =
                connectionString |> Chat.FileSystem |> Chat.init

            let initRequestStorage () =
                connectionString |> Request.FileSystem |> Request.init

            return
                { initTelegramClient = initTelegramClient
                  initTelegramChatStorage = initChatStorage
                  initRequestStorage = initRequestStorage }
        }
