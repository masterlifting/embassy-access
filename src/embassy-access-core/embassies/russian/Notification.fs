module internal EmbassyAccess.Embassies.Russian.Message

open Infrastructure
open EmbassyAccess.Domain
open EmbassyAccess.Notification

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

let send (deps: Domain.SendMessageDeps) notification =
    match notification with
    | Appointments request -> createShareAppointments request
    | Confirmations request -> createSendConfirmations request
    |> Option.map (fun request ->
        "RUSSIAN_TELEGRAM_BOT_TOKEN"
        |> Web.Telegram.Domain.CreateBy.TokenEnvVar
        |> Web.Domain.Telegram
        |> Web.Client.create
        |> ResultAsync.wrap (deps.sendRequest request))
    |> Option.defaultValue (async { return Ok() })

let receive (deps: Domain.ReceiveMessageDeps) listener =
    async { return Ok() }