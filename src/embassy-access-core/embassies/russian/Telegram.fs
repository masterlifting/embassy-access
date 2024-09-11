module internal EmbassyAccess.Embassies.Russian.Telegram

open Infrastructure
open Infrastructure.Logging
open EmbassyAccess.Domain
open EmbassyAccess.Notification
open Web.Telegram.Domain

let private createShareAppointments request =
    match request.State with
    | Completed _ ->
        match request.Appointments.IsEmpty with
        | true -> None
        | false -> Some <| Send.Request.ShareAppointments(request.Embassy, request.Appointments)
    | _ -> None

let private createSendConfirmations request =
    match request.State with
    | Completed _ ->
        match request.Appointments.IsEmpty with
        | true -> None
        | false ->
            match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
            | [] -> None
            | confirmations -> Some <| Send.Request.SendConfirmations(request.Id, confirmations)
    | _ -> None

let send message sender =
    match message with
    | Appointments request -> createShareAppointments request
    | Confirmations request -> createSendConfirmations request
    |> Option.map (fun request ->
        "RUSSIAN_TELEGRAM_BOT_TOKEN"
        |> TokenEnvVar
        |> Web.Domain.Telegram
        |> Web.Client.create
        |> ResultAsync.wrap (request |> sender))
    |> Option.defaultValue (async { return Ok() })

let receive ct listener =
    match listener with
    | Listener.Message messageType ->
        match messageType with
        | Text message ->
            async {
                $"{message}" |> Log.debug
                return Ok()
            }
        | _ -> async { return Error <| NotSupported $"Message type: {messageType}" }
    | _ -> async { return Error <| NotSupported $"Listener type: {listener}" }
