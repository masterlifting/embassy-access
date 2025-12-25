[<RequireQualifiedAccess>]
module internal EA.Worker.Shared.Worker

open Infrastructure.Prelude

[<RequireQualifiedAccess>]
module Task =
    open Microsoft.Extensions.Configuration

    type Persistence = {
        ConnectionString: string
        EncryptionKey: string
    }

    type Dependencies = {
        Configuration: IConfigurationRoot
        Persistence: Persistence
    } with

        static member create configuration = {
            Configuration = configuration
            Persistence = {
                ConnectionString = Configuration.ENVIRONMENTS.PostgresConnection
                EncryptionKey = Configuration.ENVIRONMENTS.EncryptionKey
            }
        }

module internal Dependencies =

    open Persistence.Domain
    open Persistence.Storages.Domain
    open Worker.Dependencies
    open Worker.DataAccess.Storage

    let private resultAsync = ResultAsyncBuilder()

    let create handlers configuration =
        resultAsync {

            let! tasks =
                TasksTree.Postgre {
                    String = Configuration.ENVIRONMENTS.PostgresConnection
                    Lifetime = Transient
                }
                |> TasksTree.init
                |> ResultAsync.wrap (fun storage ->
                    storage
                    |> TasksTree.Query.get
                    |> ResultAsync.apply (fun _ -> storage |> TasksTree.dispose |> Ok))

            let taskDeps = Task.Dependencies.create configuration

            let storage =
                Persistence.Storage.Connection.Database {
                    Database = Database.Postgre Configuration.ENVIRONMENTS.PostgresConnection
                    Lifetime = Singleton
                }

            let deps: Worker.Dependencies<Task.Dependencies> = {
                Name = "EA.Worker"
                RootTaskId = "WRK"
                Storage = storage
                Tasks = Some tasks
                Handlers = handlers
                TaskDeps = taskDeps
            }

            return deps |> Ok |> async.Return
        }
