open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Prelude.Tree.Builder
open Infrastructure.Configuration.Domain
open Persistence.Storages.Domain
open EA.Worker
open Worker.Domain

let private resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =

    Logging.Client.getLevel () |> Logging.Client.Console |> Logging.Client.init

    resultAsync {
        let! configuration =
            { Files = [ "appsettings.yml" ] }
            |> Configuration.Client.Yaml
            |> Configuration.Client.init
            |> async.Return

        let version =
            configuration
            |> Configuration.Client.getSection<string> "Version"
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

        let handlers =
            Tree.Node.create ("WRK", Some Initializer.run)
            |> withChildren [

                Tree.Node.create ("RUS", None)
                |> withChild (
                    Tree.Node.create ("SRB", None)
                    |> withChild (Tree.Node.create ("SA", Some Embassies.Russian.Kdmid.SearchAppointments.handle))
                )

                Tree.Node.create ("ITA", None)
                |> withChild (
                    Tree.Node.create ("SRB", None)
                    |> withChild (Tree.Node.create ("SA", Some Embassies.Italian.Prenotami.SearchAppointments.handle))
                )
            ]

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
