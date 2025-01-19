[<AutoOpen>]
module internal EA.Worker.Domain.RouteNode

open Infrastructure.Domain
open Worker.Domain

type RouteNode =
    | Name of id: Graph.NodeId * name: string

    interface Graph.INode with
        member this.Id =
            match this with
            | Name(id, _) -> id

        member this.Name =
            match this with
            | Name(_, name) -> name

        member _.set(id, name) = Name(id, name)

    static member register(nodeId, handler) =
        fun router ->
            let rec innerLoop (node: Graph.Node<RouteNode>) =
                let handler =
                    { Id = node.ShortId
                      Name = node.ShortName
                      Handler =
                        match node.Children.IsEmpty, node.ShortId = nodeId with
                        | false, _ -> None
                        | true, false -> None
                        | true, true -> handler |> Some }

                Graph.Node(handler, node.Children |> List.map innerLoop)

            router |> innerLoop
