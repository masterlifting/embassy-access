open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Configuration
open Worker
open Worker.DataAccess
open Worker.Domain
open EA.Worker

[<Literal>]
let private APP_NAME = "Worker"

[<EntryPoint>]
let main _ =
    
    System.Console.OutputEncoding <- System.Text.Encoding.UTF8
    
    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    let rootHandler =
        { Id = "WRK" |> Graph.NodeIdValue
          Name = APP_NAME
          Handler = Initializer.run |> Some }

    let appHandlers = Graph.Node(rootHandler, [ Embassies.Russian.register () ])

    let getTaskNode handlers =
        fun nodeId ->
            { SectionName = APP_NAME
              Configuration = configuration }
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

    workerConfig |> Worker.start |> Async.RunSynchronously

    0
