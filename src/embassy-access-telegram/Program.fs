open System.Threading
open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open EA.Telegram.Dependencies

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    resultAsync {
        let! configuration =
            Configuration.Client.Yaml {
                Files = [ "appsettings.yml"; "embassies.yml"; "services.yml" ]
            }
            |> Configuration.Client.init
            |> async.Return

        Some configuration
        |> Logging.Client.setLevel
        |> Logging.Client.Console
        |> Logging.Client.init

        let version =
            configuration
            |> Configuration.Client.getValue<string> "VERSION"
            |> Option.defaultValue "unknown"

        Logging.Log.inf $"EA.Telegram version: %s{version}"

        do! EA.Telegram.Initializer.run ()

        return
            Client.Dependencies.create configuration CancellationToken.None
            |> ResultAsync.wrap EA.Telegram.Client.start
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Telegram: %s{error.Message}")
