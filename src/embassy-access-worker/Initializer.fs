module internal EA.Worker.Initializer

open EA.Worker.Dependencies
open EA.Core.DataAccess
open Worker.Domain
open Infrastructure.Prelude
open Worker.DataAccess.Storage

let private resultAsync = ResultAsyncBuilder()
let run (task: ActiveTask, deps: WorkerTask.Dependencies, ct) =
    resultAsync {
        do! Postgre.Request.Migrations.apply Configuration.ENVIRONMENTS.PostgresConnection
        let! tasks =
            TasksTree.Configuration {
                Section = "Worker"
                Provider = deps.Configuration
            }
            |> TasksTree.init
            |> ResultAsync.wrap TasksTree.Query.get

        let! a = Persistence.Storages.Postgre.Provider.init {
            String = Configuration.ENVIRONMENTS.PostgresConnection
            Lifetime = Persistence.Domain.Transient
        }
        |> ResultAsync.wrap (fun client -> client |> Worker.DataAccess.Postgre.TasksTree.Command.insert tasks)
        |> ResultAsync.map ignore

        return Ok () |> async.Return
    }

