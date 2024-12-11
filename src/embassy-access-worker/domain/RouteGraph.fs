[<AutoOpen>]
module internal EA.Worker.Domain.RouteGraph

open System
open Infrastructure.Domain
open Worker.Domain

type RouteGraph =
    | Name of string

    interface Graph.INodeName with
        member _.Id = String.Empty |> Graph.NodeIdValue

        member this.Name =
            match this with
            | Name name -> name

        member _.set(_, name) = Name name

    static member register(name, handler) =
        fun router ->
            let rec innerLoop (node: Graph.Node<RouteGraph>) =
                let handler =
                    { Name = node.ShortName
                      Handler =
                        match node.Children.IsEmpty, node.ShortName = name with
                        | false, _ -> None
                        | true, false -> None
                        | true, true -> handler |> Some }

                Graph.Node(handler, node.Children |> List.map innerLoop)

            router |> innerLoop
