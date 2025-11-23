open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Domain
open Worker.Domain
open Worker.DataAccess.Storage
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
            |> Configuration.Client.getValue<string> "VERSION"
            |> Option.defaultValue "unknown"

        Logging.Log.inf $"EA.Worker version: %s{version}"

        // let! tasks =
        //     TasksTree.Configuration {
        //         Section = "Worker"
        //         Provider = configuration
        //     }
        //     |> TasksTree.init
        //     |> ResultAsync.wrap TasksTree.Query.get

        let! tasksStorage =
            TasksTree.Postgre {
                String = Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Singleton
            }
            |> TasksTree.init
            |> async.Return

        let handlers = EA.Worker.Handlers.register ()

        return
            Worker.Client.start {
                Name = $"EA.Worker: v{version}"
                RootTaskId = "WRK" |> WorkerTaskId.create
                Storage = tasksStorage
                Handlers = handlers
                Configuration = configuration
            }
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Worker: %s{error.Message}")
