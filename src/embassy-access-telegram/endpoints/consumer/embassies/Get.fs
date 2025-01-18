module EA.Telegram.Endpoints.Consumer.Embassies.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Request =
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
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; id |] -> id |> Graph.NodeIdValue |> Request.Embassy |> Ok
        | [| "01" |] -> Embassies |> Ok
        | [| "02"; embassyId; serviceId |] ->
            (embassyId |> Graph.NodeIdValue, serviceId |> Graph.NodeIdValue)
            |> Request.EmbassyService
            |> Ok
        | [| "03"; id |] -> id |> Graph.NodeIdValue |> Request.EmbassyServices |> Ok
        | _ -> $"'{parts}' of Embassies.GetRequest endpoint" |> NotSupported |> Error
