module EA.Telegram.Endpoints.Consumer.Embassies.Core

open Infrastructure.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | Embassy of Graph.NodeId
    | Embassies
    | EmbassyService of embassyId: Graph.NodeId * serviceId: Graph.NodeId
    | EmbassyServices of Graph.NodeId

    member this.Value =
        match this with
        | Embassy id -> [ "00"; id.Value ]
        | Embassies -> [ "01" ]
        | EmbassyService(embassyId, serviceId) -> [ "02"; embassyId.Value; serviceId.Value ]
        | EmbassyServices id -> [ "03"; id.Value ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; id |] -> id |> Graph.NodeIdValue |> GetRequest.Embassy |> Ok
        | [| "01" |] -> Embassies |> Ok
        | [| "02"; embassyId; serviceId |] ->
            (embassyId |> Graph.NodeIdValue, serviceId |> Graph.NodeIdValue)
            |> GetRequest.EmbassyService
            |> Ok
        | [| "03"; id |] -> id |> Graph.NodeIdValue |> GetRequest.EmbassyServices |> Ok
        | _ -> $"'{parts}' of Embassies.GetRequest endpoint" |> NotSupported |> Error

type Request =
    | Get of GetRequest

    member this.Value =
        match this with
        | Get r -> r.Value

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '0' -> parts |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' of Embassies endpoint" |> NotSupported |> Error
