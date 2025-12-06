open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open EA.Worker.Dependencies

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    resultAsync {
        let! configuration =
            Configuration.Client.Yaml { Files = [ "appsettings.yml" ] }
            |> Configuration.Client.init
            |> async.Return

        Some configuration
        |> Logging.Client.setLevel
        |> Logging.Client.Console
        |> Logging.Client.init

        let version =
            configuration
            |> Configuration.Client.getValue "VERSION"
            |> Option.defaultValue "unknown"

        Logging.Log.inf $"EA.Worker version: %s{version}"

        let workerHandlers = EA.Worker.Handlers.register ()
        let! workerDeps = Worker.Dependencies.create workerHandlers configuration

        return workerDeps |> Worker.Client.start |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Worker: %s{error.Message}")
