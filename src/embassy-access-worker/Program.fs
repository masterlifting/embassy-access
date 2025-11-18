open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Storages.Domain
open Worker.Domain

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    resultAsync {
        let! configuration =
            { Files = [ "appsettings.yml" ] }
            |> Configuration.Client.Yaml
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

        let! tasks =
            {
                Configuration.Connection.Section = "Worker"
                Configuration.Connection.Provider = configuration
            }
            |> Worker.DataAccess.TasksTree.StorageType.Configuration
            |> Worker.DataAccess.TasksTree.init
            |> ResultAsync.wrap Worker.DataAccess.TasksTree.get

        let handlers = EA.Worker.Handlers.register ()

        let! workerTasks = tasks |> Worker.Client.merge handlers |> async.Return

        return
            Worker.Client.start {
                Name = $"EA.Worker: v{version}"
                Configuration = configuration
                RootTaskId = "WRK" |> WorkerTaskId.create
                findTask = fun taskId -> workerTasks |> Tree.findNode taskId.NodeId |> Ok |> async.Return
            }
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Worker: %s{error.Message}")
