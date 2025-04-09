open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Storages.Domain
open Worker.DataAccess
open Worker.Domain
open EA.Worker

[<Literal>]
let private APP_NAME = "Worker"

[<EntryPoint>]
let main _ =

    let configuration =
        Configuration.setYamls [
            @"settings\appsettings"
            @"settings\worker"
            @"settings\embassies"
            @"settings\embassies.rus"
        ]

    Logging.useConsole configuration

    let rootNodeId = "WRK" |> Graph.NodeIdValue

    let rootHandler = {
        Id = rootNodeId
        Name = APP_NAME
        Handler = Initializer.run |> Some
    }

    let initWorkerStorage () =
        {
            Configuration.Connection.Section = APP_NAME
            Configuration.Connection.Provider = configuration
        }
        |> TaskGraph.Configuration
        |> TaskGraph.init

    let appHandlers =
        Graph.Node(rootHandler, [ Embassies.Russian.register rootNodeId initWorkerStorage ])

    let getTaskNode handlers =
        fun nodeId ->
            initWorkerStorage ()
            |> ResultAsync.wrap (TaskGraph.merge handlers)
            |> ResultAsync.map (Graph.DFS.tryFindById nodeId)
            |> ResultAsync.bind (function
                | Some node -> Ok node
                | None -> $"Task handler Id '%s{nodeId.Value}' not found." |> NotFound |> Error)

    let workerConfig = {
        Name = rootHandler.Name
        Configuration = configuration
        TaskNodeRootId = rootHandler.Id
        getTaskNode = getTaskNode appHandlers
    }

    workerConfig |> Worker.Client.start |> Async.RunSynchronously

    0
