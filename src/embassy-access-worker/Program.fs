﻿open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Storages.Domain
open Worker.Dependencies
open Worker.DataAccess
open Worker.Domain
open EA.Worker

let resultAsync = ResultAsyncBuilder()

[<EntryPoint>]
let main _ =
    resultAsync {
        let! configuration =
            {
                Files = [ "appsettings.yaml"; "worker.yaml"; "embassies.yaml"; "services.yaml" ]
            }
            |> Configuration.Client.Yaml
            |> Configuration.Client.init
            |> async.Return

        configuration
        |> Logging.Client.tryFindLevel
        |> Logging.Client.Console
        |> Logging.Client.init

        let! taskGraph =
            {
                Configuration.Connection.Section = "Worker"
                Configuration.Connection.Provider = configuration
            }
            |> TaskGraph.Configuration
            |> TaskGraph.init
            |> ResultAsync.wrap TaskGraph.get

        let rootTaskHandler = {
            Id = "WRK" |> Graph.NodeIdValue
            Handler = Initializer.run |> Some
        }

        let taskHandlers =
            Graph.Node(
                rootTaskHandler,
                [
                    taskGraph |> Embassies.Russian.createHandlers rootTaskHandler
                    taskGraph |> Embassies.Italian.createHandlers rootTaskHandler
                ]
                |> List.choose id
            )

        return
            Worker.Client.start {
                Name = "Embassy Access Worker"
                Configuration = configuration
                RootTaskId = rootTaskHandler.Id
                tryFindTask =
                    fun taskId ->
                        taskGraph
                        |> Worker.Client.registerHandlers taskHandlers
                        |> Graph.DFS.tryFind taskId
                        |> Ok
                        |> async.Return
            }
            |> Async.map (fun _ -> 0 |> Ok)
    }
    |> Async.RunSynchronously
    |> Result.defaultWith (fun error -> failwithf $"Failed to start EA.Worker: %s{error.Message}")
