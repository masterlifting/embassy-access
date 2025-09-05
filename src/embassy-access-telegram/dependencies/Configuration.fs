[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Configuration

open Infrastructure
open Infrastructure.Domain

let getEnv name =
    Configuration.Client.getEnv name
    |> Result.bind (function
        | Some value -> Ok value
        | None -> $"Environment variable '{name}' is not set." |> NotFound |> Error)

type Environments = {
    PostgresConnection: string
    TelegramBotToken: string
    AntiCaptchaApiKey: string
    OpenAIApiKey: string
    DataEncryptionKey: string
} with
    static member init () =
        let getEnv key = 
            match getEnv key with
            | Ok v -> v
            | Error e -> failwith e.Message

        {
            PostgresConnection = getEnv "POSTGRES_CONNECTION"
            TelegramBotToken = getEnv "TELEGRAM_BOT_TOKEN"
            AntiCaptchaApiKey = getEnv "ANTICAPTCHA_API_KEY"
            OpenAIApiKey = getEnv "OPENAI_API_KEY"
            DataEncryptionKey = getEnv "DATA_ENCRYPTION_KEY"
        }

let ENVIRONMENTS = Environments.init()