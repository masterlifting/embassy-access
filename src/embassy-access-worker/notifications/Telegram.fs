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

let private (|HasEmbassy|HasCountry|HasCity|HasPayload|None|) value =
    match value with
    | AP.IsString value ->
        let data = value.Split '|'

        match data.Length with
        | 1 -> HasEmbassy(data.[0])
        | 2 -> HasCountry(data.[0], data.[1])
        | 3 -> HasCity(data.[0], data.[1], data.[2])
        | 4 -> HasPayload(data.[0], data.[1], data.[2], data.[3])
        | _ -> None
    | _ -> None

module private Sender =
    open Web.Telegram.Domain.Send
    open Persistence.Domain
    open Worker.Domain
    open EmbassyAccess.Embassies.Russian.Domain

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

    let sendEmbassies ct chatId =
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

    let sendCountries ct msgId chatId embassy =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.filter (fun e -> e.Name = embassy)
            |> Seq.map _.Country
            |> Seq.map (fun c -> (embassy + "|" + c.Name), c.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = $"Countries for '{embassy}' embassy."
              Columns = 3
              Data = data }

        let message =
            { Id = msgId |> Replace
              ChatId = chatId
              Value = buttons }
            |> Buttons

        message |> Web.Telegram.Client.send ct

    let sendCities ct msgId chatId embassy country =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.filter (fun e -> e.Name = embassy)
            |> Seq.map _.Country
            |> Seq.filter (fun c -> c.Name = country)
            |> Seq.map _.City
            |> Seq.map (fun c -> (embassy + "|" + country + "|" + c.Name), c.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = $"Cities for '{embassy}' embassy in '{country}'."
              Columns = 3
              Data = data }

        let message =
            { Id = msgId |> Replace
              ChatId = chatId
              Value = buttons }
            |> Buttons

        message |> Web.Telegram.Client.send ct

    let sendPayload ct msgId chatId embassy country city =
        let msgValue =
            match embassy |> Mapper.Embassy.create country city with
            | Error error -> error.Message
            | Ok embassy ->
                match embassy with
                | Domain.Russian _ ->
                    $"Send your payload using the following format: '{embassy}|{country}|{city}|your_link_here'."
                | _ -> $"Not supported embassy: '{embassy}'."

        let message =
            { Id = msgId |> Replace
              ChatId = chatId
              Value = msgValue }
            |> Text

        message |> Web.Telegram.Client.send ct

    let createDeps ct =
        let deps = ModelBuilder()

        deps {
            let config = { TimeShift = 0y }

            let! storage = Persistence.Storage.create InMemory

            return (storage, config, ct)
        }

    let processPayload ct msgId chatId embassy country city payload =
        match embassy |> Mapper.Embassy.create country city with
        | Error error -> async { return Error error }
        | Ok embassy ->
            match embassy with
            | Domain.Russian country ->
                let request =
                    { Id = RequestId.New
                      Payload = payload
                      Embassy = Russian country
                      State = Created
                      Attempt = 0
                      ConfirmationState = Disabled
                      Appointments = Set.empty
                      Description = None
                      GroupBy = Some "Passports"
                      Modified = System.DateTime.UtcNow }

                createDeps ct
                |> Result.map Deps.Russian.processRequest
                |> ResultAsync.wrap (fun deps -> request |> Api.processRequest deps)
                |> ResultAsync.map (fun request ->
                    { Id = msgId |> Replace
                      ChatId = chatId
                      Value = request.State |> string }
                    |> Text)
                |> ResultAsync.bind (Web.Telegram.Client.send ct)
            | _ -> async { return Error <| NotSupported $"'{embassy}'." }

    let createMessage message =
        match message with
        | SendAppointments request -> request |> createAppointmentsMsg
        | SendConfirmations request -> request |> createConfirmationsMsg

    let send ct data =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> ResultAsync.wrap (data |> Web.Telegram.Client.send ct)

module private Receiver =
    open Web.Telegram.Domain.Receive

    let private receiveText ct (msg: Message<string>) client =
        match msg.Value with
        | "/start" -> client |> Sender.sendEmbassies ct msg.ChatId
        | _ -> async { return Error <| NotSupported $"Message text: {msg.Value}" }

    let private receiveCallback ct msg client =
        match msg.Value with
        | HasEmbassy embassy -> client |> Sender.sendCountries ct msg.Id msg.ChatId embassy
        | HasCountry(embassy, country) -> client |> Sender.sendCities ct msg.Id msg.ChatId embassy country
        | HasCity(embassy, country, city) -> client |> Sender.sendPayload ct msg.Id msg.ChatId embassy country city
        | _ -> async { return Error <| NotSupported $"Callback data: {msg.Value}" }

    let private receiveMessage ct msg client =
        match msg with
        | Text text -> client |> receiveText ct text
        | _ -> async { return Error <| NotSupported $"Message type: {msg}" }

    let receive ct client =
        fun data ->
            match data with
            | Message msg -> client |> receiveMessage ct msg
            | CallbackQuery msg -> client |> receiveCallback ct msg
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
