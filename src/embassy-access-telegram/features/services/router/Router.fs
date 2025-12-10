module EA.Telegram.Features.Router.Services

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Shared
open EA.Telegram.Features.Services.Router

type Get =
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

type Route =
    | Get of Get
    | Russian of Russian.Method.Route
    | Italian of Italian.Method.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Russian r -> [ "1"; r.Value ]
        | Italian r -> [ "2"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.parse |> Result.map Get
        | "1" -> remaining |> Russian.Method.Route.parse |> Result.map Russian
        | "2" -> remaining |> Italian.Method.Route.parse |> Result.map Italian
        | _ -> $"'{input}' of 'Services' endpoint is not supported." |> NotSupported |> Error
