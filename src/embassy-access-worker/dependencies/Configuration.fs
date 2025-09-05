[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Configuration

open Infrastructure
open Infrastructure.Domain

let getEnv name =
    Configuration.Client.getEnv name
    |> Result.bind (function
        | Some value -> Ok value
        | None -> $"Environment variable '{name}' is not set." |> NotFound |> Error)

type Environments = {
    PostgresConnection: string
    BrowserWebApiUrl: string
    AntiCaptchaApiKey: string
    DataEncryptionKey: string
} with
    static member init () =
        let getEnv key = 
            match getEnv key with
            | Ok v -> v
            | Error e -> failwith e.Message

        {
            PostgresConnection = getEnv "POSTGRES_CONNECTION"
            BrowserWebApiUrl = getEnv "BROWSER_WEBAPI_URL"
            AntiCaptchaApiKey = getEnv "ANTICAPTCHA_API_KEY"
            DataEncryptionKey = getEnv "DATA_ENCRYPTION_KEY"
        }

let ENVIRONMENTS = Environments.init()