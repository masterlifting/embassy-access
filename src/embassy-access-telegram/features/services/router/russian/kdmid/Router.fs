module EA.Telegram.Features.Router.Services.Russian.Kdmid

open System
open Infrastructure.Domain
open Infrastructure.Prelude
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
            $"'{input}' of 'Services.Russian.Kdmid.Get' endpoint is not supported."
            |> NotSupported
            |> Error

type Post =
    | SetManualRequest of ServiceId * EmbassyId * link: string
    | SetAutoNotifications of ServiceId * EmbassyId * link: string
    | SetAutoBookingFirst of ServiceId * EmbassyId * link: string
    | SetAutoBookingLast of ServiceId * EmbassyId * link: string
    | SetAutoBookingFirstInPeriod of ServiceId * EmbassyId * start: DateTime * finish: DateTime * link: string
    | ConfirmAppointment of RequestId * AppointmentId
    | StartManualRequest of RequestId

    member this.Value =
        match this with
        | SetManualRequest(serviceId, embassyId, payload) -> [ "0"; serviceId.Value; embassyId.Value; payload ]
        | SetAutoNotifications(serviceId, embassyId, payload) -> [ "1"; serviceId.Value; embassyId.Value; payload ]
        | SetAutoBookingFirst(serviceId, embassyId, payload) -> [ "2"; serviceId.Value; embassyId.Value; payload ]
        | SetAutoBookingLast(serviceId, embassyId, payload) -> [ "3"; serviceId.Value; embassyId.Value; payload ]
        | SetAutoBookingFirstInPeriod(serviceId, embassyId, start, finish, payload) -> [
            "4"
            serviceId.Value
            embassyId.Value
            start |> String.fromDateTime
            finish |> String.fromDateTime
            payload
          ]
        | ConfirmAppointment(requestId, appointmentId) -> [ "5"; requestId.Value; appointmentId.ValueStr ]
        | StartManualRequest requestId -> [ "6"; requestId.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; payload |] ->
            SetManualRequest(
                serviceId |> Tree.NodeId.create |> ServiceId,
                embassyId |> Tree.NodeId.create |> EmbassyId,
                payload
            )
            |> Ok
        | [| "1"; serviceId; embassyId; payload |] ->
            SetAutoNotifications(
                serviceId |> Tree.NodeId.create |> ServiceId,
                embassyId |> Tree.NodeId.create |> EmbassyId,
                payload
            )
            |> Ok
        | [| "2"; serviceId; embassyId; payload |] ->
            SetAutoBookingFirst(
                serviceId |> Tree.NodeId.create |> ServiceId,
                embassyId |> Tree.NodeId.create |> EmbassyId,
                payload
            )
            |> Ok
        | [| "3"; serviceId; embassyId; payload |] ->
            SetAutoBookingLast(
                serviceId |> Tree.NodeId.create |> ServiceId,
                embassyId |> Tree.NodeId.create |> EmbassyId,
                payload
            )
            |> Ok
        | [| "4"; serviceId; embassyId; start; finish; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                SetAutoBookingFirstInPeriod(
                    serviceId |> Tree.NodeId.create |> ServiceId,
                    embassyId |> Tree.NodeId.create |> EmbassyId,
                    start,
                    finish,
                    payload
                )
                |> Ok
            | _ ->
                $"'{start}' or '{finish}' of 'Services.Russian.Kdmid.Post' endpoint is not supported."
                |> NotSupported
                |> Error
        | [| "5"; requestId; appointmentId |] ->
            ConfirmAppointment(requestId |> UUID16 |> RequestId, appointmentId |> UUID16 |> AppointmentId)
            |> Ok
        | [| "6"; requestId |] -> StartManualRequest(requestId |> UUID16 |> RequestId) |> Ok
        | _ ->
            $"'{input}' of 'Services.Russian.Kdmid.Post' endpoint is not supported."
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
            $"'{input}' of 'Services.Russian.Kdmid.Delete' endpoint is not supported."
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
            $"'{input}' of 'Services.Russian.Kdmid' endpoint is not supported."
            |> NotSupported
            |> Error
