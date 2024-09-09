module internal EmbassyAccess.Embassies.Russian.Notification

open Infrastructure
open EmbassyAccess.Domain
open EmbassyAccess.Web

let private createAppointmentsNotification request =
    match request.State with
    | Completed _ ->
        match request.Appointments.IsEmpty with
        | true -> None
        | false -> Some <| Filter.ShareAppointments(request.Embassy, request.Appointments)
    | _ -> None

let private createConfirmationsNotification request =
    match request.State with
    | Completed _ ->
        match request.Appointments.IsEmpty with
        | true -> None
        | false ->
            match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
            | [] -> None
            | confirmations -> Some <| Filter.SendConfirmations(request.Id, confirmations)
    | _ -> None

let send (deps: Domain.SendNotificationDeps) notification =
    match notification with
    | Appointments request -> createAppointmentsNotification request
    | Confirmations request -> createConfirmationsNotification request
    |> Option.map (fun filter ->
        "RUSSIAN_TELEGRAM_BOT_TOKEN"
        |> Web.Telegram.Domain.CreateBy.TokenEnvVar
        |> Web.Domain.Telegram
        |> Web.Client.create
        |> ResultAsync.wrap (deps.send filter))
    |> Option.defaultValue (async { return Ok() })
