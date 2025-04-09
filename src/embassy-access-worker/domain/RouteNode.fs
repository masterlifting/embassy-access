[<AutoOpen>]
module internal EA.Worker.Domain.RouteNode

open Infrastructure.Domain
open Worker.Domain

type RouteNode =
    | Id of Graph.NodeId

    interface Graph.INode with
        member this.Id =
            match this with
            | Id value -> value

        member this.Name = System.String.Empty

        member _.set(id, _) = Id id

    static member register(nodeId, handler) =
        fun router ->
            let rec innerLoop (node: Graph.Node<RouteNode>) =
                let handler = {
                    Id = node.ShortId
                    Name = node.ShortName
                    Handler =
                        match node.Children.IsEmpty, node.Id = nodeId with
                        | false, _ -> None
                        | true, false -> None
                        | true, true -> handler |> Some
                }

                Graph.Node(handler, node.Children |> List.map innerLoop)

            router |> innerLoop
