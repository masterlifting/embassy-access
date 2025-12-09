module EA.Telegram.Features.Services.Router.Get

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
        | Service(embassyId, serviceId) -> [ "0"; embassyId.Value; serviceId.Value ]
        | Services embassyId -> [ "1"; embassyId.Value ]
        | UserService(embassyId, serviceId) -> [ "2"; embassyId.Value; serviceId.Value ]
        | UserServices embassyId -> [ "3"; embassyId.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; embassyId; serviceId |] ->
            let embassyId = embassyId |> Tree.NodeId.create |> EmbassyId
            let serviceId = serviceId |> Tree.NodeId.create |> ServiceId
            (embassyId, serviceId) |> Service |> Ok
        | [| "1"; embassyId |] -> embassyId |> Tree.NodeId.create |> EmbassyId |> Services |> Ok
        | [| "2"; embassyId; serviceId |] ->
            let embassyId = embassyId |> Tree.NodeId.create |> EmbassyId
            let serviceId = serviceId |> Tree.NodeId.create |> ServiceId
            (embassyId, serviceId) |> UserService |> Ok
        | [| "3"; embassyId |] -> embassyId |> Tree.NodeId.create |> EmbassyId |> UserServices |> Ok
        | _ ->
            $"'{input}' of 'Services.Get' endpoint is not supported."
            |> NotSupported
            |> Error
