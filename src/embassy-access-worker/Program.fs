open Infrastructure
open Worker.Domain
open EmbassyAccess.Worker

[<EntryPoint>]
let main _ =

    let configuration = Configuration.getYaml "appsettings"

    Logging.useConsole configuration

    let rootTask =
        { Name = "Scheduler"
          Task = Temporary.createRootTask () }

    let taskHandlers =
        Graph.Node(
            rootTask,
            [ Countries.Albania.Tasks
              Countries.Bosnia.Tasks
              Countries.Finland.Tasks
              Countries.France.Tasks
              Countries.Germany.Tasks
              Countries.Hungary.Tasks
              Countries.Ireland.Tasks
              Countries.Montenegro.Tasks
              Countries.Netherlands.Tasks
              Countries.Serbia.Tasks
              Countries.Slovenia.Tasks
              Countries.Switzerland.Tasks ]
        )

    let workerConfig =
        { Name = rootTask.Name
          Configuration = configuration
          getTask = taskHandlers |> TasksStorage.getTask configuration }

    workerConfig |> Worker.Core.start |> Async.RunSynchronously

    0
