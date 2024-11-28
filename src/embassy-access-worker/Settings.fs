module internal EA.Worker.Settings

open Infrastructure
open Worker.Domain

[<Literal>]
let APP_NAME = "Worker"

let private getConfigData configuration =
    configuration
    |> Configuration.getSection<External.TaskGraph> APP_NAME
    |> Option.map Ok
    |> Option.defaultValue (Error <| NotFound $"Section '%s{APP_NAME}' in the configuration.")

let getWorkerTask configuration workerHandlers =
    fun taskName ->
        configuration
        |> getConfigData
        |> Result.bind (Worker.Graph.merge workerHandlers)
        |> Result.bind (
            Graph.DFS.tryFindByName taskName
            >> Option.map Ok
            >> Option.defaultValue (Error <| NotFound $"Task '%s{taskName}' in the configuration.")
        )
        |> async.Return
