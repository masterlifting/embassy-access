module EA.Telegram.Endpoints.Consumer.Embassies.RussianEmbassy

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

[<Literal>]
let private Delimiter = "|"

module Model =
    module Kdmid =

        type Subscribe =
            { ServiceId: Graph.NodeId
              EmbassyId: Graph.NodeId
              ConfirmationState: ConfirmationState
              Payload: string }

        type CheckAppointments =
            { ServiceId: Graph.NodeId
              EmbassyId: Graph.NodeId
              Payload: string }

        type SendAppointments =
            { ServiceId: Graph.NodeId
              EmbassyId: Graph.NodeId
              AppointmentIds: Set<AppointmentId> }

        type ConfirmAppointment =
            { RequestId: RequestId
              AppointmentId: AppointmentId }

    module Midpass =
        type CheckStatus = { Number: string }

type GetRequest =
    | KdmidCheckAppointments of RequestId

    member this.Value =
        match this with
        | KdmidCheckAppointments requestId -> [ "00"; requestId.ValueStr ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; requestId |] -> RequestId.create requestId |> Result.map GetRequest.KdmidCheckAppointments
        | _ -> $"'{parts}' of RussianEmbassy.GetRequest endpoint" |> NotSupported |> Error

type PostRequest =
    | KdmidSubscribe of Model.Kdmid.Subscribe
    | KdmidCheckAppointments of Model.Kdmid.CheckAppointments
    | KdmidSendAppointments of Model.Kdmid.SendAppointments
    | KdmidConfirmAppointment of Model.Kdmid.ConfirmAppointment
    | MidpassCheckStatus of Model.Midpass.CheckStatus

    member this.Value =
        match this with
        | KdmidCheckAppointments model -> [ "10"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
        | KdmidSubscribe model ->
            match model.ConfirmationState with
            | ConfirmationState.Disabled -> [ "11"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
            | ConfirmationState.Manual appointmentId ->
                [ "12"
                  model.ServiceId.Value
                  model.EmbassyId.Value
                  appointmentId.ValueStr
                  model.Payload ]
            | ConfirmationState.Auto option ->
                match option with
                | FirstAvailable -> [ "13"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                | LastAvailable -> [ "14"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                | DateTimeRange(start, finish) ->
                    [ "15"
                      model.ServiceId.Value
                      model.EmbassyId.Value
                      start |> string
                      finish |> string
                      model.Payload ]
        | KdmidSendAppointments model ->
            [ "16"
              model.ServiceId.Value
              model.EmbassyId.Value
              model.AppointmentIds |> Set.map _.ValueStr |> Set.toArray |> String.concat "," ]
        | KdmidConfirmAppointment model -> [ "17"; model.RequestId.ValueStr; model.AppointmentId.ValueStr ]
        | MidpassCheckStatus model -> [ "18"; model.Number ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        let inline createKdmidSubscription serviceId embassyId payload confirmation =
            { Model.Kdmid.Subscribe.ServiceId = serviceId |> Graph.NodeIdValue
              Model.Kdmid.Subscribe.EmbassyId = embassyId |> Graph.NodeIdValue
              Model.Kdmid.Subscribe.ConfirmationState = confirmation
              Model.Kdmid.Subscribe.Payload = payload }
            |> PostRequest.KdmidSubscribe
            |> Ok

        match parts with
        | [| "10"; serviceId; embassyId; payload |] ->
            { Model.Kdmid.CheckAppointments.ServiceId = serviceId |> Graph.NodeIdValue
              Model.Kdmid.CheckAppointments.EmbassyId = embassyId |> Graph.NodeIdValue
              Model.Kdmid.CheckAppointments.Payload = payload }
            |> PostRequest.KdmidCheckAppointments
            |> Ok
        | [| "11"; serviceId; embassyId; payload |] -> createKdmidSubscription serviceId embassyId payload Disabled
        | [| "12"; serviceId; embassyId; appointmentId; payload |] ->
            appointmentId
            |> AppointmentId.create
            |> Result.bind (fun appointmentId ->
                createKdmidSubscription serviceId embassyId payload (ConfirmationState.Manual appointmentId))
        | [| "13"; serviceId; embassyId; payload |] ->
            createKdmidSubscription serviceId embassyId payload (ConfirmationState.Auto FirstAvailable)
        | [| "14"; serviceId; embassyId; payload |] ->
            createKdmidSubscription serviceId embassyId payload (ConfirmationState.Auto LastAvailable)
        | [| "15"; serviceId; embassyId; start; finish; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                createKdmidSubscription
                    serviceId
                    embassyId
                    payload
                    (ConfirmationState.Auto(DateTimeRange(start, finish)))
            | _ ->
                $"start: {start} or finish: {finish} of RussianEmbassy.PostRequest endpoint"
                |> NotSupported
                |> Error
        | [| "16"; serviceId; embassyId; appointmentIds |] ->
            appointmentIds.Split ','
            |> Array.map AppointmentId.create
            |> Result.choose
            |> Result.map (fun appointmentIds ->
                { Model.Kdmid.SendAppointments.ServiceId = serviceId |> Graph.NodeIdValue
                  Model.Kdmid.SendAppointments.EmbassyId = embassyId |> Graph.NodeIdValue
                  Model.Kdmid.SendAppointments.AppointmentIds = appointmentIds |> Set.ofList })
            |> Result.map PostRequest.KdmidSendAppointments
        | [| "17"; requestId; appointmentId |] ->
            RequestId.create requestId
            |> Result.bind (fun requestId ->
                AppointmentId.create appointmentId
                |> Result.map (fun appointmentId ->
                    { Model.Kdmid.ConfirmAppointment.RequestId = requestId
                      Model.Kdmid.ConfirmAppointment.AppointmentId = appointmentId }))
            |> Result.map PostRequest.KdmidConfirmAppointment
        | [| "18"; number |] ->
            { Model.Midpass.CheckStatus.Number = number }
            |> PostRequest.MidpassCheckStatus
            |> Ok
        | _ -> $"'{parts}' of RussianEmbassy.PostRequest endpoint" |> NotSupported |> Error

type Request =
    | Get of GetRequest
    | Post of PostRequest

    member this.Value =
        match this with
        | Get r -> r.Value
        | Post r -> r.Value

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '0' -> parts |> GetRequest.parse |> Result.map Get
        | '1' -> parts |> PostRequest.parse |> Result.map Post
        | _ -> $"'{input}' of RussianEmbassy endpoint" |> NotSupported |> Error
