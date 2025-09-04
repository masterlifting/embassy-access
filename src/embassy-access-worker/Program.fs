open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Storages.Domain
open Worker.Dependencies
open Worker.Domain
open EA.Worker

let resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =
    resultAsync {
        let! configuration =
            {
                Files = [ "appsettings.yaml"; "worker.yaml"; "embassies.yaml"; "services.yaml" ]
            }
            |> Configuration.Client.Yaml
            |> Configuration.Client.init
            |> async.Return

        configuration
        |> Logging.Client.getLevel
        |> Logging.Client.Console
        |> Logging.Client.init

        let! tasksTree =
            {
                Configuration.Connection.Section = "Worker"
                Configuration.Connection.Provider = configuration
            }
            |> Worker.DataAccess.TasksTree.StorageType.Configuration
            |> Worker.DataAccess.TasksTree.init
            |> ResultAsync.wrap Worker.DataAccess.TasksTree.get

        let rootTaskHandler = {
            Id = "WRK" |> Tree.NodeIdValue
            Handler = Initializer.run |> Some
        }

        let tasksTreeHandlers =
            Tree.Node(
                rootTaskHandler,
                [
                    tasksTree |> Embassies.Russian.createHandlers rootTaskHandler.Id
                    tasksTree |> Embassies.Italian.createHandlers rootTaskHandler.Id
                ]
                |> List.choose id
            )

        return
            Worker.Client.start {
                Name = "Embassy Access Worker"
                Configuration = configuration
                RootTaskId = rootTaskHandler.Id
                tryFindTask =
                    fun taskId ->
                        tasksTree
                        |> Worker.Client.mapTasks tasksTreeHandlers
                        |> Tree.DFS.tryFind taskId
                        |> Ok
                        |> async.Return
            }
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Worker: %s{error.Message}")
