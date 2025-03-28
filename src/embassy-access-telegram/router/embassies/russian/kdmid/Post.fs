module EA.Telegram.Router.Embassies.Russian.Kdmid.Post

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Core.Domain

module Model =
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
            | ConfirmationState.Disabled -> [ "0"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
            | ConfirmationState.Manual appointmentId ->
                [ "1"
                  model.ServiceId.Value
                  model.EmbassyId.Value
                  appointmentId.ValueStr
                  model.Payload ]
            | ConfirmationState.Auto option ->
                match option with
                | FirstAvailable -> [ "2"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                | LastAvailable -> [ "3"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
                | DateTimeRange(start, finish) ->
                    [ "4"
                      model.ServiceId.Value
                      model.EmbassyId.Value
                      start |> string
                      finish |> string
                      model.Payload ]
        | CheckAppointments model -> [ "5"; model.ServiceId.Value; model.EmbassyId.Value; model.Payload ]
        | SendAppointments model ->
            [ "6"
              model.ServiceId.Value
              model.EmbassyId.Value
              model.AppointmentIds |> Set.map _.ValueStr |> Set.toArray |> String.concat "," ]
        | ConfirmAppointment model -> [ "7"; model.RequestId.ValueStr; model.AppointmentId.ValueStr ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        let inline createSubscription serviceId embassyId payload confirmation =
            { ServiceId = serviceId |> Graph.NodeIdValue
              EmbassyId = embassyId |> Graph.NodeIdValue
              ConfirmationState = confirmation
              Payload = payload }
            |> Subscribe
            |> Ok

        match parts with
        | [| "0"; serviceId; embassyId; payload |] -> createSubscription serviceId embassyId payload Disabled
        | [| "1"; serviceId; embassyId; appointmentId; payload |] ->
            appointmentId
            |> AppointmentId.parse
            |> Result.bind (fun appointmentId ->
                createSubscription serviceId embassyId payload (ConfirmationState.Manual appointmentId))
        | [| "2"; serviceId; embassyId; payload |] ->
            createSubscription serviceId embassyId payload (ConfirmationState.Auto FirstAvailable)
        | [| "3"; serviceId; embassyId; payload |] ->
            createSubscription serviceId embassyId payload (ConfirmationState.Auto LastAvailable)
        | [| "4"; serviceId; embassyId; start; finish; payload |] ->
            match start, finish with
            | AP.IsDateTime start, AP.IsDateTime finish ->
                createSubscription serviceId embassyId payload (ConfirmationState.Auto(DateTimeRange(start, finish)))
            | _ ->
                $"start: {start} or finish: {finish} of Embassies.Russian.Kdmid.Post.KdmidSubscribe endpoint"
                |> NotSupported
                |> Error
        | [| "5"; serviceId; embassyId; payload |] ->
            { ServiceId = serviceId |> Graph.NodeIdValue
              EmbassyId = embassyId |> Graph.NodeIdValue
              Payload = payload }
            |> CheckAppointments
            |> Ok
        | [| "6"; serviceId; embassyId; appointmentIds |] ->
            appointmentIds.Split ','
            |> Array.map AppointmentId.parse
            |> Result.choose
            |> Result.map (fun appointmentIds ->
                { ServiceId = serviceId |> Graph.NodeIdValue
                  EmbassyId = embassyId |> Graph.NodeIdValue
                  AppointmentIds = appointmentIds |> Set.ofList })
            |> Result.map SendAppointments
        | [| "7"; requestId; appointmentId |] ->
            RequestId.parse requestId
            |> Result.bind (fun requestId ->
                AppointmentId.parse appointmentId
                |> Result.map (fun appointmentId ->
                    { RequestId = requestId
                      AppointmentId = appointmentId }))
            |> Result.map ConfirmAppointment
        | _ -> $"'{parts}' of Embassies.Russian.Kdmid.Post endpoint" |> NotSupported |> Error
