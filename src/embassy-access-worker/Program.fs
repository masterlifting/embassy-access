open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Storages.Domain
open Worker.Dependencies
open Worker.Domain
open EA.Worker
open Infrastructure.Prelude.Tree.NodeBuilder

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    Logging.Client.getLevel () |> Logging.Client.Console |> Logging.Client.init

    resultAsync {
        let! configuration =
            {
                Files = [
                    "appsettings.yml"
                    "data/worker-tasks-tree.yml"
                    "data/embassy-tasks-tree.yml"
                    "data/service-tasks-tree.yml"
                ]
            }
            |> Configuration.Client.Yaml
            |> Configuration.Client.init
            |> async.Return

        let version =
            configuration
            |> Configuration.Client.tryGetSection<string> "Version"
            |> function
                | Some v -> v
                | None -> "unknown"

        Logging.Log.inf $"EA.Worker version: %s{version}"

        let! tasksTree =
            {
                Configuration.Connection.Section = "Worker"
                Configuration.Connection.Provider = configuration
            }
            |> Worker.DataAccess.TasksTree.StorageType.Configuration
            |> Worker.DataAccess.TasksTree.init
            |> ResultAsync.wrap Worker.DataAccess.TasksTree.get

        let tasksTreeHandlers =
            Tree.Node.Create("WRK", Initializer.run |> Some)
            |> withChildren [
                Tree.Node.Create("RUS", None)
                |> withChild (
                    Tree.Node.Create("SRB", None)
                    |> withChild (Tree.Node.Create("SA", Embassies.Russian.Kdmid.SearchAppointments.handle |> Some))
                )

                Tree.Node.Create("ITA", None)
                |> withChild (
                    Tree.Node.Create("SRB", None)
                    |> withChild (Tree.Node.Create("SA", Embassies.Italian.Prenotami.SearchAppointments.handle |> Some))
                )
            ]
            |> Tree.Root.Init

        return
            Worker.Client.start {
                Name = $"EA.Worker: v{version}"
                Configuration = configuration
                RootTaskId = "WRK"
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
