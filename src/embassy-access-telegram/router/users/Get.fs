module EA.Telegram.Router.Users.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Route =
    | UserEmbassies
    | UserEmbassy of embassyId: Graph.NodeId
    | UserEmbassyServices of embassyId: Graph.NodeId
    | UserEmbassyService of embassyId: Graph.NodeId * serviceId: Graph.NodeId

    member this.Value =
        match this with
        | UserEmbassies -> [ "0" ]
        | UserEmbassy embassyId -> [ "1"; embassyId.Value ]
        | UserEmbassyServices embassyId -> [ "2"; embassyId.Value ]
        | UserEmbassyService(embassyId, serviceId) -> [ "3"; embassyId.Value; serviceId.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0" |] -> UserEmbassies |> Ok
        | [| "1"; embassyId |] -> embassyId |> Graph.NodeIdValue |> UserEmbassy |> Ok
        | [| "2"; embassyId |] -> embassyId |> Graph.NodeIdValue |> UserEmbassyServices |> Ok
        | [| "3"; embassyId; serviceId |] ->
            (embassyId |> Graph.NodeIdValue, serviceId |> Graph.NodeIdValue)
            |> UserEmbassyService
            |> Ok
        | _ -> $"'{parts}' of Users.Get endpoint is not supported." |> NotSupported |> Error
