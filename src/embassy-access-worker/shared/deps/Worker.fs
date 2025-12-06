namespace EA.Worker.Dependencies

open Infrastructure.Prelude
open Infrastructure.Configuration.Domain

[<RequireQualifiedAccess>]
module WorkerTask =

    open Microsoft.Extensions.Configuration

    let private result = ResultBuilder()

    type Persistence = {
        ConnectionString: string
        EncryptionKey: string
    }

    type Dependencies = {
        Configuration: IConfigurationRoot
        Persistence: Persistence
    } with

        static member create configuration =
            result {
                return {
                    Configuration = configuration
                    Persistence = {
                        ConnectionString = Configuration.ENVIRONMENTS.PostgresConnection
                        EncryptionKey = Configuration.ENVIRONMENTS.EncryptionKey
                    }
                }
            }

/// Build worker dependencies (handlers, tasks tree, persistence) for Worker.Client.start
module internal App =

    open Infrastructure.Prelude
    open Infrastructure.Configuration.Domain
    open Microsoft.Extensions.Configuration
    open Persistence.Domain
    open Persistence.Storages.Domain
    open Worker.Dependencies
    open Worker.DataAccess.Storage

    let private resultAsync = ResultAsyncBuilder()

    let create handlers (configuration: IConfigurationRoot) version =
        resultAsync {
            let! tasks =
                TasksTree.Postgre {
                    String = Configuration.ENVIRONMENTS.PostgresConnection
                    Lifetime = Transient
                }
                |> TasksTree.init
                |> ResultAsync.wrap TasksTree.Query.get
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
