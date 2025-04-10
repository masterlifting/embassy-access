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
            @"settings\appsettings.yaml"
            @"settings\worker.yaml"
            @"settings\embassies.yaml"
            @"settings\embassies.rus.yaml"
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

    let rootHandler = {
        Id = "WRK" |> Graph.NodeIdValue
        Name = APP_NAME
        Handler = Initializer.run |> Some
    }

    let russianHandler =
        workerStorage
        |> Embassies.Russian.register rootHandler
        |> ResultAsync.defaultWith (fun error ->
            failwithf $"Failed to register Russian embassy handlers: %s{error.Message}")
        |> Async.RunSynchronously

    let workerHandlers =
        Graph.Node(
            rootHandler,
            russianHandler |> Option.map List.singleton |> Option.defaultValue []

        )

    let getTaskNode handlers =
        fun nodeId ->
            workerStorage
            |> TaskGraph.getWithHandlers handlers
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
