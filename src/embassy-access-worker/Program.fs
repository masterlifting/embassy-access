open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Storages.Domain
open Worker.Dependencies
open Worker.DataAccess
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
        |> Logging.Client.tryFindLevel
        |> Logging.Client.Console
        |> Logging.Client.init

        let! tasksTree =
            {
                Configuration.Connection.Section = "Worker"
                Configuration.Connection.Provider = configuration
            }
            |> TasksTree.Configuration
            |> TasksTree.init
            |> ResultAsync.wrap TasksTree.get

        let rootTaskHandler = {
            Id = "WRK" |> Tree.NodeIdValue
            Handler = Initializer.run |> Some
        }

        let taskHandlers =
            Tree.Node(
                rootTaskHandler,
                [
                    tasksTree |> Embassies.Russian.createHandlers rootTaskHandler
                    tasksTree |> Embassies.Italian.createHandlers rootTaskHandler
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
                        |> Worker.Client.registerHandlers taskHandlers
                        |> Tree.DFS.tryFind taskId
                        |> Ok
                        |> async.Return
            }
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Worker: %s{error.Message}")
