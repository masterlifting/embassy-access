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

    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    let rootHandler =
        { Id = "WRK" |> Graph.NodeIdValue
          Name = APP_NAME
          Handler = Initializer.run |> Some }

    let appHandlers = Graph.Node(rootHandler, [ Embassies.Russian.register () ])

    let getTaskNode handlers =
        fun nodeId ->
            { Configuration.Connection.SectionName = APP_NAME
              Configuration.Connection.Provider = configuration }
            |> TaskGraph.Configuration
            |> TaskGraph.init
            |> ResultAsync.wrap (TaskGraph.create handlers)
            |> ResultAsync.map (Graph.DFS.tryFindById nodeId)
            |> ResultAsync.bind (function
                | Some node -> Ok node
                | None -> $"Task Id '%s{nodeId.Value}' in the configuration" |> NotFound |> Error)

    let workerConfig =
        { Name = rootHandler.Name
          Configuration = configuration
          TaskNodeRootId = rootHandler.Id
          getTaskNode = getTaskNode appHandlers }

    workerConfig |> Worker.Client.start |> Async.RunSynchronously

    0
