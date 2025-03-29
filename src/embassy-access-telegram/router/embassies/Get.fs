module EA.Telegram.Router.Embassies.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Route =
    | Embassies
    | Embassy of Graph.NodeId
    | EmbassyServices of Graph.NodeId
    | EmbassyService of embassyId: Graph.NodeId * serviceId: Graph.NodeId

    member this.Value =
        match this with
        | Embassies -> [ "0" ]
        | Embassy id -> [ "1"; id.Value ]
        | EmbassyServices id -> [ "2"; id.Value ]
        | EmbassyService(embassyId, serviceId) -> [ "3"; embassyId.Value; serviceId.Value ]
        |> String.concat Constants.Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Router.DELIMITER

        match parts with
        | [| "0" |] -> Embassies |> Ok
        | [| "1"; id |] -> id |> Graph.NodeIdValue |> Embassy |> Ok
        | [| "2"; id |] -> id |> Graph.NodeIdValue |> EmbassyServices |> Ok
        | [| "3"; embassyId; serviceId |] ->
            (embassyId |> Graph.NodeIdValue, serviceId |> Graph.NodeIdValue)
            |> EmbassyService
            |> Ok
        | _ -> $"'{parts}' of Embassies.Get endpoint is not supported." |> NotSupported |> Error
