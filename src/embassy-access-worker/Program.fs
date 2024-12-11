open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Configuration
open Worker.Domain
open EA.Worker

[<Literal>]
let private APP_NAME = "Worker"

[<EntryPoint>]
let main _ =

    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    let rootTask =
        { Name = APP_NAME
          Handler = Initializer.run |> Some }

    let workerHandlers = Graph.Node(rootTask, [ Embassies.Russian.register () ])

    let getTaskNode handlers =
        fun name ->
            { SectionName = APP_NAME
              Configuration = configuration }
            |> Worker.DataAccess.TaskGraph.Configuration
            |> Worker.DataAccess.TaskGraph.init
            |> ResultAsync.wrap (Worker.DataAccess.TaskGraph.get handlers)
            |> ResultAsync.map (Graph.DFS.tryFindByName name)
            |> ResultAsync.bind (function
                | Some node -> Ok node
                | None -> $"'%s{name}' in the configuration" |> NotFound |> Error)

    let workerConfig =
        { Name = rootTask.Name
          Configuration = configuration
          getTaskNode = getTaskNode workerHandlers }

    workerConfig |> Worker.Core.start |> Async.RunSynchronously

    0
