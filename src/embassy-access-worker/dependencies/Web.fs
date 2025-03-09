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

    static member create cfg =
        let result = ResultBuilder()

        result {

            let initTelegramClient () =
                cfg
                |> Configuration.getSection<string> EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN_KEY
                |> Option.map (fun token -> { Token = token } |> Web.Telegram.Client.init)
                |> Option.defaultValue (
                    $"'{EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN_KEY}' in the configuration."
                    |> NotFound
                    |> Error
                )

            let! telegramClient = initTelegramClient ()

            return { TelegramClient = telegramClient }
        }
