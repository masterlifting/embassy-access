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

    let workerStorage =
        {
            Configuration.Connection.Section = APP_NAME
            Configuration.Connection.Provider = configuration
        }
        |> TaskGraph.Configuration
        |> TaskGraph.init
        |> Result.defaultWith (fun error -> failwithf $"Failed to initialize worker storage: %s{error.Message}")

    let rootNodeId = "WRK" |> Graph.NodeIdValue

    let rootHandler = {
        Id = rootNodeId
        Name = APP_NAME
        Handler = Initializer.run |> Some
    }

    let russianHandler =
        workerStorage
        |> Embassies.Russian.register rootNodeId
        |> ResultAsync.defaultWith (fun error -> failwithf $"Failed to register Russian embassy handlers: %s{error.Message}")

    let workerHandlers =
        Graph.Node(rootHandler, [ russianHandler ])

    let getTaskNode handlers =
        fun nodeId ->
            workerStorage
            |> TaskGraph.merge handlers
            |> ResultAsync.map (Graph.DFS.tryFindById nodeId)
            |> ResultAsync.bind (function
                | Some node -> Ok node
                | None -> $"Task handler Id '%s{nodeId.Value}' not found." |> NotFound |> Error)

    let workerConfig = {
        Name = rootHandler.Name
        Configuration = configuration
        TaskNodeRootId = rootHandler.Id
        getTaskNode = getTaskNode workerHandlers
    }

    workerConfig |> Worker.Client.start |> Async.RunSynchronously

    0
