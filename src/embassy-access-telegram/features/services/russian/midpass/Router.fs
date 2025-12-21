module EA.Telegram.Features.Router.Services.Russian.Midpass

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Shared

type Get =
    | Info of RequestId
    | Menu of RequestId

    member this.Value =
        match this with
        | Info requestId -> [ "0"; requestId.Value ]
        | Menu requestId -> [ "1"; requestId.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> Info(requestId |> UUID16 |> RequestId) |> Ok
        | [| "1"; requestId |] -> Menu(requestId |> UUID16 |> RequestId) |> Ok
        | _ ->
            $"'{input}' of 'Services.Russian.Midpass.Get' endpoint is not supported."
            |> NotSupported
            |> Error

type Post =
    | CheckStatus of ServiceId * EmbassyId * number: string

    member this.Value =
        match this with
        | CheckStatus(serviceId, embassyId, number) ->
            [ "0"; serviceId.Value; embassyId.Value; number ]
            |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; number |] ->
            CheckStatus(
                serviceId |> Tree.NodeId.create |> ServiceId,
                embassyId |> Tree.NodeId.create |> EmbassyId,
                number
            )
            |> Ok
        | _ ->
            $"'{input}' of 'Services.Russian.Midpass.Post' endpoint is not supported."
            |> NotSupported
            |> Error

type Delete =
    | Subscription of RequestId

    member this.Value =
        match this with
        | Subscription requestId -> [ "0"; requestId.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> Subscription(requestId |> UUID16 |> RequestId) |> Ok
        | _ ->
            $"'{input}' of 'Services.Russian.Midpass.Delete' endpoint is not supported."
            |> NotSupported
            |> Error

type Route =
    | Get of Get
    | Post of Post
    | Delete of Delete

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Post r -> [ "1"; r.Value ]
        | Delete r -> [ "2"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.parse |> Result.map Get
        | "1" -> remaining |> Post.parse |> Result.map Post
        | "2" -> remaining |> Delete.parse |> Result.map Delete
        | _ ->
            $"'{input}' of 'Services.Russian.Midpass' endpoint is not supported."
            |> NotSupported
            |> Error
