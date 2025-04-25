module EA.Telegram.Router.Services.Russian.Kdmid.Post

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | CheckSlotsNow of ServiceId * EmbassyId * link: string
    | SlotsAutoNotification of ServiceId * EmbassyId * link: string
    | BookFirstSlot of ServiceId * EmbassyId * link: string
    | BookLastSlot of ServiceId * EmbassyId * link: string
    | BookFirstSlotInPeriod of ServiceId * EmbassyId * start: DateTime * finish: DateTime * link: string
    | ConfirmAppointment of RequestId * AppointmentId

    member this.Value =
        match this with
        | CheckSlotsNow(serviceId, embassyId, payload) -> [ "0"; serviceId.ValueStr; embassyId.ValueStr; payload ]
        | SlotsAutoNotification(serviceId, embassyId, payload) -> [
            "1"
            serviceId.ValueStr
            embassyId.ValueStr
            payload
          ]
        | BookFirstSlot(serviceId, embassyId, payload) -> [ "2"; serviceId.ValueStr; embassyId.ValueStr; payload ]
        | BookLastSlot(serviceId, embassyId, payload) -> [ "3"; serviceId.ValueStr; embassyId.ValueStr; payload ]
        | BookFirstSlotInPeriod(serviceId, embassyId, start, finish, payload) -> [
            "4"
            serviceId.ValueStr
            embassyId.ValueStr
            (start |> String.fromDateTime)
            (finish |> String.fromDateTime)
            payload
          ]
        | ConfirmAppointment(requestId, appointmentId) -> [ "5"; requestId.ValueStr; appointmentId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; payload |] ->
            CheckSlotsNow(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
                payload
            )
            |> Ok
        | [| "1"; serviceId; embassyId; payload |] ->
            SlotsAutoNotification(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
                payload
            )
            |> Ok
        | [| "2"; serviceId; embassyId; payload |] ->
            BookFirstSlot(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
                payload
            )
            |> Ok
        | [| "3"; serviceId; embassyId; payload |] ->
            BookLastSlot(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
                payload
            )
            |> Ok
        | [| "4"; serviceId; embassyId; start; finish; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                BookFirstSlotInPeriod(
                    serviceId |> Graph.NodeIdValue |> ServiceId,
                    embassyId |> Graph.NodeIdValue |> EmbassyId,
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
        | _ ->
            $"'{parts}' of 'Services.Russian.Kdmid.Post' endpoint is not supported."
            |> NotSupported
            |> Error
