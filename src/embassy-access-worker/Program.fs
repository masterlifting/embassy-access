open Infrastructure
open Worker.Domain
open EA.Worker

[<EntryPoint>]
let main _ =

    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    let rootTask =
        { Id = Graph.NodeId.New
          Name = Settings.APP_NAME
          Task = Initializer.initialize |> Some }

    let workerHandlers = Graph.Node(rootTask, [ Embassies.Russian.register () ])

    let workerConfig =
        { Name = rootTask.Name
          Configuration = configuration
          getTask = workerHandlers |> Settings.getWorkerTask configuration }

    workerConfig |> Worker.Core.start |> Async.RunSynchronously

    0
