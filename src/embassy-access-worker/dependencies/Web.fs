[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Web

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Domain
open Web.Telegram.Domain
open EA.Worker.Domain.Constants

type Dependencies =
    { TelegramClient: TelegramClient }

    static member create() =
        let result = ResultBuilder()

        result {

            let initTelegramClient () =
                Configuration.getEnvVar TELEGRAM_BOT_TOKEN_KEY
                |> Result.bind (function
                    | Some token -> { Token = token } |> Web.Telegram.Client.init
                    | None -> $"'{TELEGRAM_BOT_TOKEN_KEY}' in the configuration." |> NotFound |> Error)

            let! telegramClient = initTelegramClient ()

            return { TelegramClient = telegramClient }
        }
