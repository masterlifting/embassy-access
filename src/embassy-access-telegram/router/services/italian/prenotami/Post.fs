module EA.Telegram.Router.Services.Italian.Prenotami.Post

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | SetManualRequest of ServiceId * EmbassyId * login: string * password: string
    | SetAutoNotifications of ServiceId * EmbassyId * login: string * password: string
    | ConfirmAppointment of RequestId * AppointmentId
    | StartManualRequest of RequestId

    member this.Value =
        match this with
        | SetManualRequest(serviceId, embassyId, login, password) -> [
            "0"
            serviceId.ValueStr
            embassyId.ValueStr
            login
            password
          ]
        | SetAutoNotifications(serviceId, embassyId, login, password) -> [
            "1"
            serviceId.ValueStr
            embassyId.ValueStr
            login
            password
          ]
        | ConfirmAppointment(requestId, appointmentId) -> [ "2"; requestId.ValueStr; appointmentId.ValueStr ]
        | StartManualRequest(requestId) -> [ "3"; requestId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; login; password |] ->
            SetManualRequest(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
                login,
                password
            )
            |> Ok
        | [| "1"; serviceId; embassyId; login; password |] ->
            SetAutoNotifications(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
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
