open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Storages.Domain
open Worker.Dependencies
open Worker.DataAccess
open Worker.Domain
open EA.Worker

[<Literal>]
let private APP_NAME = "Worker"

[<EntryPoint>]
let main _ =

    let configuration =
        Configuration.setYamls [
            @"settings\appsettings.yaml"
            @"settings\worker.yaml"
            @"settings\embassies.yaml"
            @"settings\embassies.rus.yaml"
        ]

    Logging.useConsole configuration

    let taskGraphStorage =
        {
            Configuration.Connection.Section = APP_NAME
            Configuration.Connection.Provider = configuration
        }
        |> TaskGraph.Configuration
        |> TaskGraph.init
        |> Result.defaultWith (fun error -> failwithf $"Failed to initialize task graph storage: %s{error.Message}")

    let taskGraph =
        taskGraphStorage
        |> TaskGraph.get
        |> ResultAsync.defaultWith (fun error -> failwithf $"Failed to initialize task graph: %s{error.Message}")
        |> Async.RunSynchronously

    let rootHandler = {
        Id = "WRK" |> Graph.NodeIdValue
        Handler = Initializer.run |> Some
    }

    let handlers =
        Graph.Node(rootHandler, [ taskGraph |> Embassies.Russian.createHandlers rootHandler ] |> List.choose id)

    let tryFindTask () =
        fun nodeId ->
            taskGraph
            |> Worker.Client.registerHandlers handlers
            |> Graph.DFS.tryFindById nodeId
            |> Ok
            |> async.Return

    let workerDeps: Worker.Dependencies = {
        Name = APP_NAME
        Configuration = configuration
        RootTaskId = rootHandler.Id
        tryFindTask = tryFindTask ()
    }

    workerDeps |> Worker.Client.start |> Async.RunSynchronously

    0
