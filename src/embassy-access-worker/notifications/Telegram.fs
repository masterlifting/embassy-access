[<RequireQualifiedAccess>]
module EmbassyAccess.Worker.Notifications.Telegram

open System
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Notifications
open Infrastructure
open Web.Telegram.Domain
open Web.Telegram.Domain.Send

let private AdminChatId = 379444553L

let private send' ct msg =
    EnvKey "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
    |> Web.Telegram.Client.create
    |> ResultAsync.wrap (msg |> Web.Telegram.Client.send ct)

let private createAppointmentsMsg request =
    Create.appointmentsNotification request
    |> Option.map (fun (embassy, appointments) ->
        let value: Buttons =
            { Name = $"Found Appointments for {embassy}"
              Columns = 3
              Data =
                appointments
                |> Seq.map (fun x -> x.Value, x.Description |> Option.defaultValue "No data")
                |> Map.ofSeq }

        { Id = New
          ChatId = AdminChatId
          Value = value }
        |> Buttons)

let private createConfirmationsMsg request =
    Create.confirmationsNotification request
    |> Option.map (fun (requestId, embassy, confirmations) ->
        let value = confirmations |> Seq.map _.Description |> String.concat "\n"

        { Id = New
          ChatId = AdminChatId
          Value = value }
        |> Text)

let private createMessage message =
    match message with
    | SendAppointments request -> request |> createAppointmentsMsg
    | SendConfirmations request -> request |> createConfirmationsMsg

let send ct notification =
    notification |> createMessage |> Option.map (send' ct)
