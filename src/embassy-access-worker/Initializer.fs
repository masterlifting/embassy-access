module internal EA.Worker.Initializer

open EA.Worker.Dependencies
open EA.Core.DataAccess
open Persistence.Domain
open Persistence.Storages
open Worker.Domain
open Infrastructure.Prelude
open Worker.DataAccess
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

        return
            Postgre.Provider.init {
                String = Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Transient
            }
            |> ResultAsync.wrap (fun client ->
                client
                |> Postgre.TasksTree.Command.insert tasks
                |> ResultAsync.apply (client |> Postgre.Provider.dispose |> Ok |> async.Return))
    }
