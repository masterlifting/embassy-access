module EA.Telegram.Features.Router.Services.Italian.Prenotami

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
            $"'{input}' of 'Services.Italian.Prenotami.Post' endpoint is not supported."
            |> NotSupported
            |> Error

type Post =
    | SetManualRequest of ServiceId * EmbassyId * login: string * password: string
    | SetAutoNotifications of ServiceId * EmbassyId * login: string * password: string
    | ConfirmAppointment of RequestId * AppointmentId
    | StartManualRequest of RequestId

    member this.Value =
        match this with
        | SetManualRequest(serviceId, embassyId, login, password) -> [
            "0"
            serviceId.Value
            embassyId.Value
            login
            password
          ]
        | SetAutoNotifications(serviceId, embassyId, login, password) -> [
            "1"
            serviceId.Value
            embassyId.Value
            login
            password
          ]
        | ConfirmAppointment(requestId, appointmentId) -> [ "2"; requestId.Value; appointmentId.ValueStr ]
        | StartManualRequest requestId -> [ "3"; requestId.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; login; password |] ->
            SetManualRequest(
                serviceId |> Tree.NodeId.create |> ServiceId,
                embassyId |> Tree.NodeId.create |> EmbassyId,
                login,
                password
            )
            |> Ok
        | [| "1"; serviceId; embassyId; login; password |] ->
            SetAutoNotifications(
                serviceId |> Tree.NodeId.create |> ServiceId,
                embassyId |> Tree.NodeId.create |> EmbassyId,
                login,
                password
            )
            |> Ok
        | [| "2"; requestId; appointmentId |] ->
            ConfirmAppointment(requestId |> UUID16 |> RequestId, appointmentId |> UUID16 |> AppointmentId)
            |> Ok
        | [| "3"; requestId |] -> StartManualRequest(requestId |> UUID16 |> RequestId) |> Ok
        | _ ->
            $"'{input}' of 'Services.Italian.Prenotami.Post' endpoint is not supported."
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
            $"'{input}' of 'Services.Italian.Prenotami.Delete' endpoint is not supported."
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
            $"'{input}' of 'Services.Italian.Prenotami' endpoint is not supported."
            |> NotSupported
            |> Error
