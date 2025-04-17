module EA.Telegram.Router.Services.Get

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | Service of EmbassyId * ServiceId
    | Services of EmbassyId
    | UserService of EmbassyId * ServiceId
    | UserServices of EmbassyId

    member this.Value =
        match this with
        | Service(embassyId, serviceId) -> [ "0"; embassyId.ValueStr; serviceId.ValueStr ]
        | Services embassyId -> [ "1"; embassyId.ValueStr ]
        | UserService(embassyId, serviceId) -> [ "2"; embassyId.ValueStr; serviceId.ValueStr ]
        | UserServices embassyId -> [ "3"; embassyId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; embassyId; serviceId |] ->
            let embassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
            let serviceId = serviceId |> Graph.NodeIdValue |> ServiceId
            (embassyId, serviceId) |> Service |> Ok
        | [| "1"; embassyId |] -> embassyId |> Graph.NodeIdValue |> EmbassyId |> Services |> Ok
        | [| "2"; embassyId; serviceId |] ->
            let embassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
            let serviceId = serviceId |> Graph.NodeIdValue |> ServiceId
            (embassyId, serviceId) |> Service |> Ok
        | [| "3"; embassyId |] -> embassyId |> Graph.NodeIdValue |> EmbassyId |> UserServices |> Ok
        | _ ->
            $"'{parts}' of 'Services.Get' endpoint is not supported."
            |> NotSupported
            |> Error
