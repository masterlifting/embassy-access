open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    Logging.Client.getLevel () |> Logging.Client.Console |> Logging.Client.init

    resultAsync {
        let! configuration =
            { Files = [ "appsettings.yaml" ] }
            |> Configuration.Client.Yaml
            |> Configuration.Client.init
            |> async.Return

        let version =
            configuration
            |> Configuration.Client.getSection<string> "Version"
            |> Option.defaultValue "unknown"

        Logging.Log.inf $"EA.Telegram version: %s{version}"

        return 0 |> Ok |> async.Return
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Telegram: %s{error.Message}")
