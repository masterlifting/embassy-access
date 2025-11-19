[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Configuration

open Infrastructure
open Infrastructure.Domain

let getEnv name =
    Configuration.Client.getEnvValue name
    |> Result.bind (function
        | Some value -> Ok value
        | None -> $"Environment variable '{name}' is not set." |> NotFound |> Error)

type Environments = {
    EncryptionKey: string
    AntiCaptchaKey: string
    BrowserWebApiUrl: string
    PostgresConnection: string
} with

    static member init() =
        let getEnv key =
            match getEnv key with
            | Ok v -> v
            | Error e -> failwith e.Message

        {
            EncryptionKey = getEnv "DATA_ENCRYPTION_KEY"
            AntiCaptchaKey = getEnv "ANTICAPTCHA_KEY"
            BrowserWebApiUrl = getEnv "BROWSER_WEBAPI_URL"
            PostgresConnection = getEnv "POSTGRES_CONNECTION"
        }

let ENVIRONMENTS = Environments.init ()
