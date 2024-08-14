module internal EmbassyAccess.Worker.TasksStorage

open Infrastructure
open Infrastructure.Configuration

[<Literal>]
let private sectionName = "Worker"

let private buildGraph rootNode configuration =
    configuration
    |> getSection<Worker.Domain.External.TaskGraph> sectionName
    |> Option.map (Worker.Graph.create rootNode)
    |> Option.defaultValue (Error <| NotFound $"Section '%s{sectionName}' in the configuration.")

let getTask configuration taskHandlers=
    fun taskName ->
        async {
            return
                buildGraph taskHandlers configuration
                |> Result.bind (fun graph ->
                    Graph.findNode taskName graph
                    |> Option.map Ok
                    |> Option.defaultValue (
                        Error
                        <| NotFound $"Task '%s{taskName}' in the section '%s{sectionName}' of the configuration."
                    ))
        }
