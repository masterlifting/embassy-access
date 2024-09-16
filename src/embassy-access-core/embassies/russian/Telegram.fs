module internal EmbassyAccess.Embassies.Russian.Telegram

open EmbassyAccess.Domain
open Web.Telegram.Domain.Send


let private createAppointmentsMsg request =
    Core.createAppointmentsNotification request
    |> Option.map (fun (embassy, appointments) ->
        let value: Buttons =
            { Name = $"Found Appointments for {embassy}"
              Columns = 3
              Data =
                appointments
                |> Seq.map (fun x -> x.Value, x.Description |> Option.defaultValue "No data")
                |> Map.ofSeq }

        { Id = None
          ChatId = EmbassyAccess.Notification.Telegram.AdminChatId
          Value = value }
        |> Buttons)

let private createConfirmationsMsg request =
    Core.createConfirmationsNotification request
    |> Option.map (fun (requestId, embassy, confirmations) ->
        let value = confirmations |> Seq.map _.Description |> String.concat "\n"

        { Id = None
          ChatId = EmbassyAccess.Notification.Telegram.AdminChatId
          Value = value }
        |> Text)

let createMessage message =
    match message with
    | SendAppointments request -> request |> createAppointmentsMsg
    | SendConfirmations request -> request |> createConfirmationsMsg
