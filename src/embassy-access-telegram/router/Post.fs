module EA.Telegram.Router.Embassies.Post

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Core.Domain

module Model =
    type Subscribe = {
        ServiceId: Graph.NodeId
        EmbassyId: Graph.NodeId
        IsBackground: bool
        ConfirmationState: ConfirmationState
        Payload: string
    } with

        member this.Serialize() = [
            this.ServiceId.Value
            this.EmbassyId.Value
            match this.IsBackground with
            | true -> "1"
            | false -> "0"
            this.Payload
        ]

        static member deserialize (parts: string list) confirmationState =
            match parts with
            | [ serviceId; embassyId; isBackground; payload ] ->
                match isBackground with
                | "0" -> false |> Ok
                | "1" -> true |> Ok
                | _ ->
                    $"'{isBackground}' of Embassies.Russian.Kdmid.Post.Subscribe endpoint is not supported."
                    |> NotSupported
                    |> Error
                |> Result.map (fun isBackground -> {
                    ServiceId = serviceId |> Graph.NodeIdValue
                    EmbassyId = embassyId |> Graph.NodeIdValue
                    Payload = payload
                    ConfirmationState = confirmationState
                    IsBackground = isBackground
                })
            | _ ->
                $"'{parts}' of Embassies.Russian.Kdmid.Post.Subscribe endpoint is not supported."
                |> NotSupported
                |> Error

    type CheckAppointments = {
        ServiceId: Graph.NodeId
        EmbassyId: Graph.NodeId
        Payload: string
    } with

        member this.Serialize() = [ this.ServiceId.Value; this.EmbassyId.Value; this.Payload ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; payload ] ->
                {
                    ServiceId = serviceId |> Graph.NodeIdValue
                    EmbassyId = embassyId |> Graph.NodeIdValue
                    Payload = payload
                }
                |> Ok
            | _ ->
                $"'{parts}' of Embassies.Russian.Kdmid.Post.CheckAppointments endpoint is not supported."
                |> NotSupported
                |> Error

    type SendAppointments = {
        ServiceId: Graph.NodeId
        EmbassyId: Graph.NodeId
        AppointmentIds: Set<AppointmentId>
    } with

        member this.Serialize() = [
            this.ServiceId.Value
            this.EmbassyId.Value
            this.AppointmentIds |> Set.map _.ValueStr |> Set.toArray |> String.concat ","
        ]

        static member deserialize(parts: string list) =
            match parts with
            | [ serviceId; embassyId; appointmentIds ] ->
                appointmentIds.Split ','
                |> Array.map AppointmentId.parse
                |> Array.toList
                |> Result.choose
                |> Result.map (fun appointmentIds -> {
                    ServiceId = serviceId |> Graph.NodeIdValue
                    EmbassyId = embassyId |> Graph.NodeIdValue
                    AppointmentIds = appointmentIds |> Set.ofList
                })
            | _ ->
                $"'{parts}' of Embassies.Russian.Kdmid.Post.SendAppointments endpoint is not supported."
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
                $"'{parts}' of Embassies.Russian.Kdmid.Post.ConfirmAppointment endpoint is not supported."
                |> NotSupported
                |> Error

open Model

type Route =
    | Subscribe of Subscribe
    | CheckAppointments of CheckAppointments
    | SendAppointments of SendAppointments
    | ConfirmAppointment of ConfirmAppointment

    member this.Value =
        match this with
        | Subscribe model ->
            match model.ConfirmationState with
            | ConfirmationState.Disabled -> "0" :: model.Serialize()
            | ConfirmationState.Appointment appointmentId -> [ "1"; appointmentId.ValueStr ] @ model.Serialize()
            | ConfirmationState.FirstAvailable -> "2" :: model.Serialize()
            | ConfirmationState.LastAvailable -> "3" :: model.Serialize()
            | ConfirmationState.DateTimeRange(start, finish) ->
                [ "4"; start |> String.fromDateTime; finish |> String.fromDateTime ]
                @ model.Serialize()
        | CheckAppointments model -> "5" :: model.Serialize()
        | SendAppointments model -> "6" :: model.Serialize()
        | ConfirmAppointment model -> "7" :: model.Serialize()
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; isBackground; payload |] ->
            ConfirmationState.Disabled
            |> Subscribe.deserialize [ serviceId; embassyId; isBackground; payload ]
            |> Result.map Route.Subscribe
        | [| "1"; appointmentId; serviceId; embassyId; isBackground; payload |] ->
            appointmentId
            |> AppointmentId.parse
            |> Result.bind (fun appointmentId ->
                appointmentId
                |> ConfirmationState.Appointment
                |> Subscribe.deserialize [ serviceId; embassyId; isBackground; payload ])
            |> Result.map Route.Subscribe
        | [| "2"; serviceId; embassyId; payload; isBackground |] ->
            ConfirmationState.FirstAvailable
            |> Subscribe.deserialize [ serviceId; embassyId; isBackground; payload ]
            |> Result.map Route.Subscribe
        | [| "3"; serviceId; embassyId; payload; isBackground |] ->
            ConfirmationState.LastAvailable
            |> Subscribe.deserialize [ serviceId; embassyId; isBackground; payload ]
            |> Result.map Route.Subscribe
        | [| "4"; start; finish; serviceId; embassyId; isBackground; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                (start, finish)
                |> ConfirmationState.DateTimeRange
                |> Subscribe.deserialize [ serviceId; embassyId; isBackground; payload ]
                |> Result.map Route.Subscribe
            | _ ->
                $"Start: '{start}' or Finish: '{finish}' of Embassies.Russian.Kdmid.Post.KdmidSubscribe endpoint is not supported."
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
            $"'{parts}' of Embassies.Russian.Kdmid.Post endpoint is not supported."
            |> NotSupported
            |> Error
