[<RequireQualifiedAccess>]
module EA.Core.Persistence.Query

open EA.Core.Domain

module Request =

    type TryFindOne =
        | ById of RequestId
        | FirstByName of string
        | SingleByName of string

    type FindMany =
        | ByIds of Set<RequestId>
        | ByEmbassyName of string

    module internal InMemory =
        open Infrastructure

        module FindOne =
            let byId (id: RequestId) (requests: Request list) =
                requests |> Seq.tryFind (fun x -> x.Id = id) |> Ok

            let first name (requests: Request list) =
                requests |> Seq.tryFind (fun x -> x.Service.Embassy.Name = name) |> Ok

            let single name (requests: Request list) =
                requests
                |> Seq.filter (fun x -> x.Service.Name = name)
                |> fun result ->
                    match result |> Seq.length with
                    | 1 -> result |> Seq.tryHead |> Ok
                    | _ -> $"Single request for {name} not found." |> NotSupported |> Error

        module FindMany =
            let byIds ids (requests: Request list) =
                requests |> Seq.filter (fun x -> ids |> Set.exists (fun id -> x.Id = id)) |> Ok

            let byEmbassyName name (requests: Request list) =
                requests |> Seq.filter (fun x -> x.Service.Embassy.Name = name) |> Ok
