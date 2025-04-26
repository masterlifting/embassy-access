module EA.Telegram.Router.Services.Italian.Prenotami.Post

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | CheckSlotsNow of ServiceId * EmbassyId * login: string * password: string
    | SlotsAutoNotification of ServiceId * EmbassyId * login: string * password: string
    | ConfirmAppointment of RequestId * AppointmentId

    member this.Value =
        match this with
        | CheckSlotsNow(serviceId, embassyId, login, password) -> [
            "0"
            serviceId.ValueStr
            embassyId.ValueStr
            login
            password
          ]
        | SlotsAutoNotification(serviceId, embassyId, login, password) -> [
            "1"
            serviceId.ValueStr
            embassyId.ValueStr
            login
            password
          ]
        | ConfirmAppointment(requestId, appointmentId) -> [ "2"; requestId.ValueStr; appointmentId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; serviceId; embassyId; login; password |] ->
            CheckSlotsNow(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
                login,
                password
            )
            |> Ok
        | [| "1"; serviceId; embassyId; login; password |] ->
            SlotsAutoNotification(
                serviceId |> Graph.NodeIdValue |> ServiceId,
                embassyId |> Graph.NodeIdValue |> EmbassyId,
                login,
                password
            )
            |> Ok
        | [| "2"; requestId; appointmentId |] ->
            ConfirmAppointment(requestId |> UUID16 |> RequestId, appointmentId |> UUID16 |> AppointmentId)
            |> Ok
        | _ ->
            $"'{input}' of 'Services.Italian.Prenotami.Post' endpoint is not supported."
            |> NotSupported
            |> Error
