[<RequireQualifiedAccess>]
module internal EA.Telegram.Dependencies.Configuration

open Infrastructure
open Infrastructure.Domain

let getEnv name =
    Configuration.Client.getEnvValue name
    |> Result.bind (function
        | Some value -> Ok value
        | None -> $"Environment variable '{name}' is not set." |> NotFound |> Error)

type Environments = {
    PostgresConnection: string
    BrowserWebApiUrl: string
    TelegramBotToken: string
    AntiCaptchaApiKey: string
    OpenAIApiKey: string
    EncryptionKey: string
} with

    static member init() =
        let getEnv key =
            match getEnv key with
            | Ok v -> v
            | Error e -> failwith e.Message

        {
            PostgresConnection = getEnv "POSTGRES_CONNECTION"
            BrowserWebApiUrl = getEnv "BROWSER_WEBAPI_URL"
            TelegramBotToken = getEnv "TELEGRAM_BOT_TOKEN"
            AntiCaptchaApiKey = getEnv "ANTICAPTCHA_API_KEY"
            OpenAIApiKey = getEnv "OPENAI_API_KEY"
            EncryptionKey = getEnv "DATA_ENCRYPTION_KEY"
        }

let ENVIRONMENTS = Environments.init ()
