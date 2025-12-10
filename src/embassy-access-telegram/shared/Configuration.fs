[<RequireQualifiedAccess>]
module internal EA.Telegram.Shared.Configuration

open Infrastructure
open Infrastructure.Domain

let getEnv name =
    Configuration.Client.getEnvValue name
    |> Result.bind (function
        | Some value -> Ok value
        | None -> $"Environment variable '{name}' is not set." |> NotFound |> Error)

type Environments = {
    OpenAIKey: string
    EncryptionKey: string
    AntiCaptchaKey: string
    TelegramBotToken: string
    BrowserWebApiUrl: string
    PostgresConnection: string
} with

    static member init() =
        let getEnv key =
            match getEnv key with
            | Ok v -> v
            | Error e -> failwith e.Message

        {
            OpenAIKey = getEnv "OPENAI_KEY"
            AntiCaptchaKey = getEnv "ANTICAPTCHA_KEY"
            EncryptionKey = getEnv "DATA_ENCRYPTION_KEY"
            TelegramBotToken = getEnv "TELEGRAM_BOT_TOKEN"
            BrowserWebApiUrl = getEnv "BROWSER_WEBAPI_URL"
            PostgresConnection = getEnv "POSTGRES_CONNECTION"
        }

let ENVIRONMENTS = Environments.init ()
