[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Web

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients
open Web.Clients.Domain
open EA.Worker.Domain.Constants

type Dependencies = {
    TelegramClient: Telegram.Client
} with

    static member create() =
        let result = ResultBuilder()

        result {

            let initTelegramClient () =
                Configuration.Client.tryGetEnv TELEGRAM_BOT_TOKEN_KEY None
                |> Result.bind (function
                    | Some token -> { Telegram.Token = token } |> Telegram.Provider.init
                    | None -> $"The environment '{TELEGRAM_BOT_TOKEN_KEY}'" |> NotFound |> Error)

            let! telegramClient = initTelegramClient ()

            return { TelegramClient = telegramClient }
        }
