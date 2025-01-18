module EA.Telegram.Endpoints.Consumer.Users.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Request =
    | UserEmbassies
    | UserEmbassy of embassyId: Graph.NodeId
    | UserEmbassyServices of embassyId: Graph.NodeId
    | UserEmbassyService of embassyId: Graph.NodeId * serviceId: Graph.NodeId

    member this.Value =
        match this with
        | UserEmbassies -> [ "00" ]
        | UserEmbassy embassyId -> [ "01"; embassyId.Value ]
        | UserEmbassyServices embassyId -> [ "02"; embassyId.Value ]
        | UserEmbassyService(embassyId, serviceId) -> [ "03"; embassyId.Value; serviceId.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(parts: string[]) =
        match parts with
        | [| "00" |] -> UserEmbassies |> Ok
        | [| "01"; embassyId |] -> embassyId |> Graph.NodeIdValue |> UserEmbassy |> Ok
        | [| "02"; embassyId |] -> embassyId |> Graph.NodeIdValue |> UserEmbassyServices |> Ok
        | [| "03"; embassyId; serviceId |] ->
            (embassyId |> Graph.NodeIdValue, serviceId |> Graph.NodeIdValue)
            |> UserEmbassyService
            |> Ok
        | _ -> $"'{parts}' of Users.GetRequest endpoint" |> NotSupported |> Error
