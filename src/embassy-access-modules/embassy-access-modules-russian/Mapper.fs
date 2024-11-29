[<RequireQualifiedAccess>]
module EA.Embassies.Russian.Mapper

open Infrastructure
open EA.Embassies.Russian.Domain

module ServiceInfo =
    let rec toGraph (graph: External.ServiceInfo) =
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
                      Instruction = graph.Instruction },
                    children
                )))
