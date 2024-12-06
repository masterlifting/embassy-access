﻿[<AutoOpen>]
module internal EA.Worker.Domain.Route

open System
open Infrastructure
open Worker.Domain

type Route =
    | Name of string

    interface Graph.INodeName with
        member _.Id = String.Empty |> Graph.NodeIdValue

        member this.Name =
            match this with
            | Name name -> name

        member _.set(_, name) = Name name

    static member register(name, handler) =
        fun router ->
            let rec innerLoop (node: Graph.Node<Route>) =
                let handler =
                    { Name = node.ShortName
                      Handler =
                        match node.Children.IsEmpty, node.ShortName = name with
                        | false, _ -> None
                        | true, false -> None
                        | true, true -> handler |> Some }

                Graph.Node(handler, node.Children |> List.map innerLoop)

            router |> innerLoop
