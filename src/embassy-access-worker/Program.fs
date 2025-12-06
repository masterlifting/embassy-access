open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Domain
open Persistence.Storages.Domain
open EA.Worker.Dependencies
open Worker.DataAccess.Storage

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

        let! tasks =
            TasksTree.Postgre {
                String = Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Transient
            }
            |> TasksTree.init
            |> ResultAsync.wrap TasksTree.Query.get

        let handlers = EA.Worker.Handlers.register ()
        let! taskDeps = WorkerTask.Dependencies.create configuration |> async.Return
        let workerStorage =
            Persistence.Storage.Connection.Database {
                Database = Database.Postgre Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Singleton
            }

        return
            Worker.Client.start {
                Name = $"EA.Worker: v{version}"
                RootTaskId = "WRK"
                Storage = workerStorage
                Tasks = Some tasks
                Handlers = handlers
                TaskDeps = taskDeps
            }
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Worker: %s{error.Message}")
