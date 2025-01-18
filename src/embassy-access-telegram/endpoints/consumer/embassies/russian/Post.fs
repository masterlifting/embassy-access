module EA.Telegram.Endpoints.Consumer.Embassies.Russian.Post

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Core.Domain

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

type Request =
    | KdmidSubscribe of Model.Kdmid.Subscribe
    | KdmidCheckAppointments of Model.Kdmid.CheckAppointments
    | KdmidSendAppointments of Model.Kdmid.SendAppointments
    | KdmidConfirmAppointment of Model.Kdmid.ConfirmAppointment

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
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(parts: string[]) =
        let inline createKdmidSubscription serviceId embassyId payload confirmation =
            { Model.Kdmid.Subscribe.ServiceId = serviceId |> Graph.NodeIdValue
              Model.Kdmid.Subscribe.EmbassyId = embassyId |> Graph.NodeIdValue
              Model.Kdmid.Subscribe.ConfirmationState = confirmation
              Model.Kdmid.Subscribe.Payload = payload }
            |> Request.KdmidSubscribe
            |> Ok

        match parts with
        | [| "10"; serviceId; embassyId; payload |] ->
            { Model.Kdmid.CheckAppointments.ServiceId = serviceId |> Graph.NodeIdValue
              Model.Kdmid.CheckAppointments.EmbassyId = embassyId |> Graph.NodeIdValue
              Model.Kdmid.CheckAppointments.Payload = payload }
            |> Request.KdmidCheckAppointments
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
            |> Result.map Request.KdmidSendAppointments
        | [| "17"; requestId; appointmentId |] ->
            RequestId.create requestId
            |> Result.bind (fun requestId ->
                AppointmentId.create appointmentId
                |> Result.map (fun appointmentId ->
                    { Model.Kdmid.ConfirmAppointment.RequestId = requestId
                      Model.Kdmid.ConfirmAppointment.AppointmentId = appointmentId }))
            |> Result.map Request.KdmidConfirmAppointment
        | _ -> $"'{parts}' of RussianEmbassy.PostRequest endpoint" |> NotSupported |> Error
