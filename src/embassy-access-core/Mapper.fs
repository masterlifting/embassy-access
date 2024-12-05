[<RequireQualifiedAccess>]
module EA.Core.Mapper

open Infrastructure
open EA.Core.Domain

module Embassy =
    let rec toGraph (graph: External.Graph) =
        graph.Id
        |> Graph.NodeId.create
        |> Result.bind (fun nodeId ->
            match graph.Children with
            | null -> List.empty |> Ok
            | children -> children |> Seq.map toGraph |> Result.choose
            |> Result.map (fun children ->
                Graph.Node(
                    { Id = nodeId
                      Name = graph.Name
                      Description = graph.Description },
                    children
                )))