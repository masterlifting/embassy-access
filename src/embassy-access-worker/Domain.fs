module internal EA.Worker.Domain

open Infrastructure
open Worker.Domain

type WorkerRoute =
    | Name of string

    interface Graph.INodeName with
        member this.Id = Graph.NodeId.New

        member this.Name =
            match this with
            | Name name -> name

        member this.setName name = Name name

    static member register (name, handler) router =

        let rec innerLoop (node: Graph.Node<WorkerRoute>) =
            let handler =
                { Id = node.Id
                  Name = node.ShortName
                  Task =
                    match node.Children.IsEmpty, node.ShortName = name with
                    | false, _ -> None
                    | true, false -> None
                    | true, true -> handler |> Some }

            Graph.Node(handler, node.Children |> List.map innerLoop)

        router |> innerLoop
