module EA.Telegram.Router.Services.Russian.Kdmid.Post

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Core.Domain

module Models =

    type CreateSubscription = {
        Url: string
        ServiceId: ServiceId
        EmbassyId: EmbassyId
        Confirmation: Confirmation
        UseBackground: bool
    } with

        member this.Serialize() = [
            this.ServiceId.ValueStr
            this.EmbassyId.ValueStr
            match this.UseBackground with
            | true -> "1"
            | false -> "0"
            this.Url
        ]

        static member deserialize (parts: string list) confirmation =
            match parts with
            | [ serviceId; embassyId; useBackground; payload ] ->
                match useBackground with
                | "0" -> false |> Ok
                | "1" -> true |> Ok
                | _ ->
                    $"'{useBackground}' of 'Services.Russian.Kdmid.Post.Subscribe' endpoint is not supported."
                    |> NotSupported
                    |> Error
                |> Result.map (fun useBackground -> {
                    Url = payload
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                    Confirmation = confirmation
                    UseBackground = useBackground
                })
            | _ ->
                $"'{parts}' of 'Services.Russian.Kdmid.Post.Subscribe' endpoint is not supported."
                |> NotSupported
                |> Error

    type CheckAppointments = {
        Uri: string
        ServiceId: ServiceId
        EmbassyId: EmbassyId
    } with

        member this.Serialize() = [ this.ServiceId.ValueStr; this.EmbassyId.ValueStr; this.Uri ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; payload ] ->
                {
                    Uri = payload
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                }
                |> Ok
            | _ ->
                $"'{parts}' of 'Services.Russian.Kdmid.Post.CheckAppointments' endpoint is not supported."
                |> NotSupported
                |> Error

    type SendAppointments = {
        ServiceId: ServiceId
        EmbassyId: EmbassyId
        Appointments: Set<AppointmentId>
    } with

        member this.Serialize() = [
            this.ServiceId.ValueStr
            this.EmbassyId.ValueStr
            this.Appointments |> Set.map _.ValueStr |> Set.toArray |> String.concat ","
        ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; appointments ] ->
                appointments.Split ','
                |> Array.map AppointmentId.parse
                |> Array.toList
                |> Result.choose
                |> Result.map (fun appointmentIds -> {
                    ServiceId = serviceId |> Graph.NodeIdValue |> ServiceId
                    EmbassyId = embassyId |> Graph.NodeIdValue |> EmbassyId
                    Appointments = appointmentIds |> Set.ofList
                })
            | _ ->
                $"'{parts}' of 'Services.Russian.Kdmid.Post.SendAppointments' endpoint is not supported."
                |> NotSupported
                |> Error

    type ConfirmAppointment = {
        RequestId: RequestId
        AppointmentId: AppointmentId
    } with

        member this.Serialize() = [ this.RequestId.ValueStr; this.AppointmentId.ValueStr ]

        static member deserialize(parts: string list) =
            match parts with
            | [ requestId; appointmentId ] ->
                requestId
                |> RequestId.parse
                |> Result.bind (fun requestId ->
                    appointmentId
                    |> AppointmentId.parse
                    |> Result.map (fun appointmentId -> {
                        RequestId = requestId
                        AppointmentId = appointmentId
                    }))
            | _ ->
                $"'{parts}' of 'Services.Russian.Kdmid.Post.ConfirmAppointment' endpoint is not supported."
                |> NotSupported
                |> Error

open Models

type Route =
    | CreateSubscription of CreateSubscription
    | CheckAppointments of CheckAppointments
    | SendAppointments of SendAppointments
    | ConfirmAppointment of ConfirmAppointment
    | CheckSlotsNow of ServiceId * EmbassyId * string
    | SlotsAutoNotification of ServiceId * EmbassyId * string
    | BookFirstSlot of ServiceId * EmbassyId * string
    | BookLastSlot of ServiceId * EmbassyId * string
    | BookFirstSlotInPeriod of ServiceId * EmbassyId * string

    member this.Value =
        match this with
        | CreateSubscription model ->
            match model.Confirmation with
            | Confirmation.Disabled -> "0" :: model.Serialize()
            | Confirmation.ForAppointment appointmentId -> [ "1"; appointmentId.ValueStr ] @ model.Serialize()
            | Confirmation.FirstAvailable -> "2" :: model.Serialize()
            | Confirmation.LastAvailable -> "3" :: model.Serialize()
            | Confirmation.DateTimeRange(start, finish) ->
                [ "4"; start |> String.fromDateTime; finish |> String.fromDateTime ]
                @ model.Serialize()
        | CheckAppointments model -> "5" :: model.Serialize()
        | SendAppointments model -> "6" :: model.Serialize()
        | ConfirmAppointment model -> "7" :: model.Serialize()
        | CheckSlotsNow(serviceId, embassyId, payload) -> [ "8"; serviceId.ValueStr; embassyId.ValueStr; payload ]
        | SlotsAutoNotification(serviceId, embassyId, payload) -> [
            "9"
            serviceId.ValueStr
            embassyId.ValueStr
            payload
          ]
        | BookFirstSlot(serviceId, embassyId, payload) -> [ "10"; serviceId.ValueStr; embassyId.ValueStr; payload ]
        | BookLastSlot(serviceId, embassyId, payload) -> [ "11"; serviceId.ValueStr; embassyId.ValueStr; payload ]
        | BookFirstSlotInPeriod(serviceId, embassyId, payload) -> [
            "12"
            serviceId.ValueStr
            embassyId.ValueStr
            payload
          ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; isBackground; payload |] ->
            Confirmation.Disabled
            |> CreateSubscription.deserialize [ serviceId; embassyId; isBackground; payload ]
            |> Result.map Route.CreateSubscription
        | [| "1"; appointmentId; serviceId; embassyId; isBackground; payload |] ->
            appointmentId
            |> AppointmentId.parse
            |> Result.bind (fun appointmentId ->
                appointmentId
                |> Confirmation.ForAppointment
                |> CreateSubscription.deserialize [ serviceId; embassyId; isBackground; payload ])
            |> Result.map Route.CreateSubscription
        | [| "2"; serviceId; embassyId; payload; isBackground |] ->
            Confirmation.FirstAvailable
            |> CreateSubscription.deserialize [ serviceId; embassyId; isBackground; payload ]
            |> Result.map Route.CreateSubscription
        | [| "3"; serviceId; embassyId; payload; isBackground |] ->
            Confirmation.LastAvailable
            |> CreateSubscription.deserialize [ serviceId; embassyId; isBackground; payload ]
            |> Result.map Route.CreateSubscription
        | [| "4"; start; finish; serviceId; embassyId; isBackground; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                (start, finish)
                |> Confirmation.DateTimeRange
                |> CreateSubscription.deserialize [ serviceId; embassyId; isBackground; payload ]
                |> Result.map Route.CreateSubscription
            | _ ->
                $"Start: '{start}' or Finish: '{finish}' of 'Services.Russian.Kdmid.Post.KdmidSubscribe' endpoint is not supported."
                |> NotSupported
                |> Error
        | [| "5"; serviceId; embassyId; payload |] ->
            [ serviceId; embassyId; payload ]
            |> CheckAppointments.deserialize
            |> Result.map Route.CheckAppointments
        | [| "6"; serviceId; embassyId; appointmentIds |] ->
            [ serviceId; embassyId; appointmentIds ]
            |> SendAppointments.deserialize
            |> Result.map Route.SendAppointments
        | [| "7"; requestId; appointmentId |] ->
            [ requestId; appointmentId ]
            |> ConfirmAppointment.deserialize
            |> Result.map Route.ConfirmAppointment
        | _ ->
            $"'{parts}' of 'Services.Russian.Kdmid.Post' endpoint is not supported."
            |> NotSupported
            |> Error
