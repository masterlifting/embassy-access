module internal EA.Worker.Settings

open Infrastructure
open Worker.Domain

[<Literal>]
let AppName = "Worker"

let getTaskGraphConfig configuration =
    configuration
    |> Configuration.getSection<External.TaskGraph> AppName
    |> Option.map Ok
    |> Option.defaultValue (Error <| NotFound $"Section '%s{AppName}' in the configuration.")

let getTask configuration workerHandlers =
    fun taskName ->
        configuration
        |> getTaskGraphConfig
        |> Result.bind (Worker.Graph.merge workerHandlers)
        |> Result.bind (
            Graph.DFS.tryFindByName taskName
            >> Option.map Ok
            >> Option.defaultValue (Error <| NotFound $"Task '%s{taskName}' in the configuration.")
        )
        |> async.Return
