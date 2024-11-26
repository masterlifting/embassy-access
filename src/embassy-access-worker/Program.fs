open Infrastructure
open Worker.Domain
open EA.Worker

[<EntryPoint>]
let main _ =

    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    let rootTask =
        { Id = Graph.NodeId.New
          Name = Settings.AppName
          Task = Some Initializer.initialize }

    let workerHandlers =
        Graph.Node(
            rootTask,
            [ Countries.Albania.Tasks
              Countries.Bosnia.Tasks
              Countries.Finland.Tasks
              Countries.France.Tasks
              Countries.Germany.Tasks
              Countries.Hungary.Tasks
              Countries.Ireland.Tasks
              Countries.Italy.Tasks
              Countries.Montenegro.Tasks
              Countries.Netherlands.Tasks
              Countries.Serbia.Tasks
              Countries.Slovenia.Tasks
              Countries.Switzerland.Tasks ]

        )

    let workerConfig =
        { Name = rootTask.Name
          Configuration = configuration
          getTask = workerHandlers |> Settings.getTask configuration }

    workerConfig |> Worker.Core.start |> Async.RunSynchronously

    0
