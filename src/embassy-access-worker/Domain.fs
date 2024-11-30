module internal EA.Worker.Domain

open System
open Infrastructure
open Worker.Domain

type WorkerRoute =
    | Name of string

    interface Graph.INodeName with
        member _.Id = String.Empty |> Graph.NodeIdValue

        member this.Name =
            match this with
            | Name name -> name

        member _.set(_, name) = Name name

    static member register (name, handler) router =

        let rec innerLoop (node: Graph.Node<WorkerRoute>) =
            let handler =
                { Name = node.ShortName
                  Task =
                    match node.Children.IsEmpty, node.ShortName = name with
                    | false, _ -> None
                    | true, false -> None
                    | true, true -> handler |> Some }

            Graph.Node(handler, node.Children |> List.map innerLoop)

        router |> innerLoop

module Error =
    let ofList (errors: Error' list) =
        match errors.Length with
        | 0 -> "Errors in the error list" |> NotFound
        | 1 -> errors[0]
        | _ ->
            let errors =
                errors
                |> Seq.mapi (fun i error -> $"%i{i}. %s{error.MessageEx}")
                |> String.concat Environment.NewLine

            Operation
                { Message = $"%s{Environment.NewLine}Multiple errors occurred:%s{Environment.NewLine}%s{errors}"
                  Code = None }
