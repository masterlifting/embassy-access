module EA.Telegram.Router.Services.Russian.Kdmid.Post

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
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
