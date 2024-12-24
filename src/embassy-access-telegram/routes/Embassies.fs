module EA.Telegram.Routes.Embassies

open Infrastructure.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | Embassy of Graph.NodeId
    | EmbassyService of embassyId: Graph.NodeId * serviceId: Graph.NodeId
    | EmbassyServices of Graph.NodeId
    | Embassies

    member this.Code =
        match this with
        | Embassy id -> [ "00"; id.Value ]
        | EmbassyService(embassyId, serviceId) -> [ "01"; embassyId.Value; serviceId.Value ]
        | EmbassyServices id -> [ "02"; id.Value ]
        | Embassies -> [ "03" ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; id |] -> id |> Graph.NodeIdValue |> GetRequest.Embassy |> Ok
        | [| "01"; embassyId; serviceId |] ->
            (embassyId |> Graph.NodeIdValue, serviceId |> Graph.NodeIdValue)
            |> GetRequest.EmbassyService
            |> Ok
        | [| "02"; id |] -> id |> Graph.NodeIdValue |> GetRequest.EmbassyServices |> Ok
        | [| "03" |] -> Embassies |> Ok
        | _ -> $"'{parts}' for Embassies.GetRequest" |> NotSupported |> Error

type Request =
    | Get of GetRequest

    member this.Route =
        match this with
        | Get r -> r.Code

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '0' -> parts |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' route of Embassies" |> NotSupported |> Error
