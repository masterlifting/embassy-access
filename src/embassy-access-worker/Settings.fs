module internal EA.Worker.Settings

open Infrastructure
open Worker.Domain

[<Literal>]
let AppName = "Worker"

[<Literal>]
let private SectionName = "Worker"

let getTaskGraphConfig configuration =
    configuration
    |> Configuration.getSection<External.TaskGraph> SectionName
    |> Option.map Ok
    |> Option.defaultValue (Error <| NotFound $"Section '%s{SectionName}' in the configuration.")

let getSchedule configuration =
    fun taskName ->
        configuration
        |> getTaskGraphConfig
        |> Result.map (Worker.Graph.createTaskGraph)
        |> Result.map (Graph.DFS.tryFindByName taskName >> Option.bind _.Value.Schedule)
        |> Result.bind (
            Option.map Ok
            >> Option.defaultValue (Error <| NotFound $"Task schedule '%s{taskName}' in the configuration.")
        )

let getTask configuration workerHandlers =
    fun taskName ->
        configuration
        |> getTaskGraphConfig
        |> Result.map (Worker.Graph.createTaskGraph)
        |> Result.bind (Worker.Graph.merge workerHandlers)
        |> Result.bind (
            Graph.DFS.tryFindByName taskName
            >> Option.map Ok
            >> Option.defaultValue (Error <| NotFound $"Task '%s{taskName}' in the configuration.")
        )
        |> async.Return
