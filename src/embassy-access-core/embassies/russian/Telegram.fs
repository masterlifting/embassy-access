module internal EmbassyAccess.Embassies.Russian.Telegram

open Infrastructure
open Infrastructure.Logging
open EmbassyAccess.Domain
open Web.Telegram.Domain

module private Sender =
    open Web.Telegram.Domain.Send

    let createAppointmentsMsg request =
        match request.State with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false ->

                let value: Buttons =
                    { Name = $"Found Appointments for {request.Embassy}"
                      Data =
                        request.Appointments
                        |> Seq.map (fun x -> x.Value, x.Description |> Option.defaultValue "No data")
                        |> Map.ofSeq }

                Buttons
                    { Id = None
                      ChatId = EmbassyAccess.Telegram.AdminChatId
                      Value = value }
                |> Some
        | _ -> None

    let createConfirmationsMsg request =
        match request.State with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false ->
                match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                | [] -> None
                | confirmations ->
                    let value = confirmations |> Seq.map _.Description |> String.concat "\n"

                    Text
                        { Id = None
                          ChatId = EmbassyAccess.Telegram.AdminChatId
                          Value = value }
                    |> Some
        | _ -> None

module private Receiver =
    open Web.Telegram.Domain.Receive

    let receiveMessage ct message =
        match message with
        | Text message ->
            async {
                $"{message}" |> Log.debug
                return Ok()
            }
        | _ -> async { return Error <| NotSupported $"Message type: {message}" }

let send ct message =
    match message with
    | SendAppointments request -> request |> Sender.createAppointmentsMsg
    | SendConfirmations request -> request |> Sender.createConfirmationsMsg
    |> Option.map (EmbassyAccess.Telegram.send ct)
    |> Option.defaultValue (async { return Ok 0 })

let receive ct data =
    match data with
    | Receive.Data.Message message -> message |> Receiver.receiveMessage ct
    | _ -> async { return Error <| NotSupported $"Listener type: {data}" }
