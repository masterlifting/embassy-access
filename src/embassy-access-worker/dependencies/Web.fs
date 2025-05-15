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
    initBrowser: unit -> Async<Result<Browser.Client, Error'>>
} with

    static member create() =
        let result = ResultBuilder()

        result {

            let! telegramClient =
                Configuration.Client.tryGetEnv TELEGRAM_BOT_TOKEN_KEY None
                |> Result.bind (function
                    | Some token -> { Telegram.Token = token } |> Telegram.Client.init
                    | None -> $"The environment '{TELEGRAM_BOT_TOKEN_KEY}' not found." |> NotFound |> Error)

            let initBrowser () =
                Browser.Client.init { Browser = Browser.Chromium }

            return {
                TelegramClient = telegramClient
                initBrowser = initBrowser
            }
        }
