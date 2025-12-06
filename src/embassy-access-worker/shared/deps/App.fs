module internal EA.Worker.Dependencies.App

open Infrastructure.Prelude
open Microsoft.Extensions.Configuration
open Infrastructure.Configuration.Domain
open Persistence.Domain
open Persistence.Storages.Domain
open EA.Worker.Dependencies
open Worker.Dependencies
open Worker.DataAccess.Storage

let private resultAsync = ResultAsyncBuilder()

/// Build worker dependencies (handlers, tasks tree, persistence) for Worker.Client.start
let create (configuration: IConfigurationRoot) version =
    resultAsync {
        let! tasks =
            TasksTree.Postgre {
                String = Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Transient
            }
            |> TasksTree.init
            |> ResultAsync.wrap TasksTree.Query.get

        let handlers = EA.Worker.Handlers.register ()
        let! taskDeps = WorkerTask.Dependencies.create configuration |> async.Return

        let workerStorage =
            Persistence.Storage.Connection.Database {
                Database = Database.Postgre Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Singleton
            }

        let deps: Worker.Dependencies<WorkerTask.Dependencies> = {
            Name = $"EA.Worker: v{version}"
            RootTaskId = "WRK"
            Storage = workerStorage
            Tasks = Some tasks
            Handlers = handlers
            TaskDeps = taskDeps
        }

        return deps |> Ok |> async.Return
    }
