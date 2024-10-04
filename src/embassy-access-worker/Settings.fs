module internal EmbassyAccess.Worker.Settings

open Infrastructure
open Worker.Domain

let get configuration =
    configuration
    |> Configuration.getSection<External.TaskGraph> "Worker"
    |> Option.map Ok
    |> Option.defaultValue (Error <| NotFound "Section 'Worker' in the configuration.")

let getSchedule configuration =
    fun taskName ->
        configuration
        |> get
        |> Result.bind Worker.Graph.map
        |> Result.map (Graph.findNode taskName >> Option.bind _.Value.Schedule)
        |> Result.bind (
            Option.map Ok
            >> Option.defaultValue (Error <| NotFound $"Task schedule '%s{taskName}' in the configuration.")
        )

let getTask taskHandlers configuration =
    fun taskName ->
        configuration
        |> get
        |> Result.bind (Worker.Graph.create taskHandlers)
        |> Result.bind (
            Graph.findNode taskName
            >> Option.map Ok
            >> Option.defaultValue (Error <| NotFound $"Task '%s{taskName}' in the configuration.")
        )
        |> async.Return
