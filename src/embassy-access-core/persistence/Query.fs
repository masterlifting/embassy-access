[<RequireQualifiedAccess>]
module EA.Core.Persistence.Query

open EA.Core.Domain

module Request =

    type GetOne =
        | ById of RequestId
        | First of string
        | Single of string

    type GetMany =
        | ByIds of Set<RequestId>
        | ByEmbassyName of string

    module internal InMemory =
        open Infrastructure

        module GetOne =
            let byId (id: RequestId) (requests: External.Request array) =
                requests |> Seq.tryFind (fun x -> x.Id = id.Value)

            let first name (requests: External.Request array) =
                requests |> Seq.tryFind (fun x -> x.Service.EmbassyName = name)

            let single name (requests: External.Request array) =
                requests
                |> Seq.filter (fun x -> x.Service.Name = name)
                |> fun result ->
                    match result |> Seq.length with
                    | 1 -> result |> Seq.head |> Ok
                    | _ -> $"Single request for {name} not found." |> NotSupported |> Error

        module GetMany =
            let byIds ids (requests: External.Request array) =
                requests |> Seq.filter (fun x -> ids |> Set.exists (fun id -> x.Id = id)) |> Ok

            let byEmbassyName name (requests: External.Request array) =
                requests |> Seq.filter (fun x -> x.Service.EmbassyName = name) |> Ok
