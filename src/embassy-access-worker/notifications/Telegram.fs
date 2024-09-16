[<RequireQualifiedAccess>]
module EmbassyAccess.Worker.Notifications.Telegram

open System
open EmbassyAccess.Domain
open EmbassyAccess.Worker.Notifications
open Infrastructure
open Web.Telegram.Domain
open EmbassyAccess

let private AdminChatId = 379444553L

[<Literal>]
let private EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

module private Sender =
    open Web.Telegram.Domain.Send

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

    let sendEmbassies ct messageId chatId =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.map (fun embassy -> embassy.Name, embassy.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = "Available Embassies."
              Columns = 3
              Data = data }

        let message =
            { Id = New
              ChatId = chatId
              Value = buttons }
            |> Buttons

        message |> Web.Telegram.Client.send ct

    let sendCountries ct messageId chatId embassyName =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.filter (fun embassy -> embassy.Name = embassyName)
            |> Seq.map _.Country
            |> Seq.map (fun country -> country.Name, country.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = $"Available Countries for '{embassyName}'."
              Columns = 3
              Data = data }

        let message =
            { Id = messageId |> Replace
              ChatId = chatId
              Value = buttons }
            |> Buttons

        message |> Web.Telegram.Client.send ct

    let createMessage message =
        match message with
        | SendAppointments request -> request |> createAppointmentsMsg
        | SendConfirmations request -> request |> createConfirmationsMsg

    let send ct data =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> ResultAsync.wrap (data |>  Web.Telegram.Client.send ct)

module private Receiver =
    open Web.Telegram.Domain.Receive

    let private receiveText ct client (msg: Message<string>) =
        match msg.Value with
        | "/start" -> client |> Sender.sendEmbassies ct msg.Id msg.ChatId
        | _ -> async { return Error <| NotSupported $"Message text: {msg.Value}" }
        
    let private (|IsEmbassy|_|) = function
        | Mapper.Embassy.Russian -> Some Mapper.Embassy.Russian
        | Mapper.Embassy.British -> Some Mapper.Embassy.British
        | Mapper.Embassy.French -> Some Mapper.Embassy.French
        | Mapper.Embassy.German -> Some Mapper.Embassy.German
        | Mapper.Embassy.Italian -> Some Mapper.Embassy.Italian
        | Mapper.Embassy.Spanish -> Some Mapper.Embassy.Spanish
        | _ -> None
        
    let private (|IsCountry|_|) = function
        | Mapper.Country.Albania -> Some Mapper.Country.Albania
        | Mapper.Country.France -> Some Mapper.Country.France
        | Mapper.Country.Germany -> Some Mapper.Country.Germany
        | Mapper.Country.Bosnia -> Some Mapper.Country.Bosnia
        | Mapper.Country.Finland -> Some Mapper.Country.Finland
        | Mapper.Country.Hungary -> Some Mapper.Country.Hungary
        | Mapper.Country.Ireland -> Some Mapper.Country.Ireland
        | Mapper.Country.Montenegro -> Some Mapper.Country.Montenegro
        | Mapper.Country.Netherlands -> Some Mapper.Country.Netherlands
        | Mapper.Country.Serbia -> Some Mapper.Country.Serbia
        | Mapper.Country.Slovenia -> Some Mapper.Country.Slovenia
        | Mapper.Country.Switzerland -> Some Mapper.Country.Switzerland
        | _ -> None

    let private receiveCallback ct client (msg: Message<string>) =
        match msg.Value with
        | IsEmbassy embassy -> client |> Sender.sendCountries ct msg.Id msg.ChatId embassy
        | _ -> async { return Error <| NotSupported $"Callback data: {msg.Value}" }

    let private receiveDataMessage ct client msg =
        match msg with
        | Text text -> text |> receiveText ct client
        | _ -> async { return Error <| NotSupported $"Message type: {msg}" }

    let receive ct client =
        fun data ->
            match data with
            | Message msg -> msg |> receiveDataMessage ct client
            | CallbackQuery msg -> msg |> receiveCallback ct client
            | _ -> async { return Error <| NotSupported $"Data type: {data}" }
            |> ResultAsync.map (fun _ -> ())

module private Listener =

    let private createListener ct =
        Result.map (fun client -> Web.Domain.Listener.Telegram(client, Receiver.receive ct client))

    let listen ct =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> createListener ct
        |> Web.Client.listen ct

let send ct notification =
    notification |> Sender.createMessage |> Option.map (Sender.send ct)

let listen = Listener.listen
