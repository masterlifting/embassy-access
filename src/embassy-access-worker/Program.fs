open Infrastructure
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Worker.Domain

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

        let! tasks =
            Worker.DataAccess.Storage.TasksTree.Configuration {
                Section = "Worker"
                Provider = configuration
            }
            |> Worker.DataAccess.Storage.TasksTree.init
            |> ResultAsync.wrap Worker.DataAccess.Storage.TasksTree.Query.get

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
