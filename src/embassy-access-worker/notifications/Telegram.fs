[<RequireQualifiedAccess>]
module EmbassyAccess.Worker.Notifications.Telegram

open System
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
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

    let private createAppointmentsMessage request =
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

    let private createConfirmationsMessage request =
        Create.confirmationsNotification request
        |> Option.map (fun (requestId, embassy, confirmations) ->
            let value = confirmations |> Seq.map _.Description |> String.concat "\n"

            { Id = New
              ChatId = AdminChatId
              Value = value }
            |> Text)

    let createEmbassiesButtons chatId =
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

        { Id = New
          ChatId = chatId
          Value = buttons }
        |> Buttons

    let createCountriesButtons msgId chatId embassy' =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.filter (fun embassy -> embassy.Name = embassy')
            |> Seq.map _.Country
            |> Seq.map (fun country -> (embassy' + "|" + country.Name), country.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = $"Countries for '{embassy'}' embassy."
              Columns = 3
              Data = data }

        { Id = msgId |> Replace
          ChatId = chatId
          Value = buttons }
        |> Buttons

    let createCitiesButtons msgId chatId (embassy', country') =
        let data =
            Api.getEmbassies ()
            |> Seq.concat
            |> Seq.map Mapper.Embassy.toExternal
            |> Seq.filter (fun embassy -> embassy.Name = embassy')
            |> Seq.map _.Country
            |> Seq.filter (fun country -> country.Name = country')
            |> Seq.map _.City
            |> Seq.map (fun city -> (embassy' + "|" + country' + "|" + city.Name), city.Name)
            |> Seq.sortBy fst
            |> Map

        let buttons: Buttons =
            { Name = $"Cities for '{embassy'}' embassy in '{country'}'."
              Columns = 3
              Data = data }

        { Id = msgId |> Replace
          ChatId = chatId
          Value = buttons }
        |> Buttons

    let createPayloadRequestMessage msgId chatId (embassy', country', city') =
        let msgValue =
            match (embassy', country', city') |> Mapper.Embassy.create with
            | Error error -> error.Message
            | Ok embassy ->
                match embassy with
                | Russian _ ->
                    $"Send your payload using the following format: '{embassy'}|{country'}|{city'}|your_link_here'."
                | _ -> $"Not supported embassy: '{embassy'}'."

        { Id = msgId |> Replace
          ChatId = chatId
          Value = msgValue }
        |> Text

    let createPayloadResponseMessage ct chatId (embassy', country', city') payload =
        let messageRes =
            (embassy', country', city')
            |> Mapper.Embassy.create
            |> ResultAsync.wrap (fun embassy ->
                match embassy with
                | Russian country ->
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
                          Modified = DateTime.UtcNow }

                    Persistence.Storage.create InMemory
                    |> ResultAsync.wrap (Repository.Command.Request.create ct request)
                    |> ResultAsync.map (fun request ->
                        { Id = New
                          ChatId = chatId
                          Value = $"Request created for '{request.Embassy}'." }
                        |> Text)
                | _ -> embassy' |> NotSupported |> Error |> async.Return)

        messageRes
        |> Async.map (function
            | Ok message -> message
            | Error error ->
                { Id = New
                  ChatId = chatId
                  Value = error.Message }
                |> Text)

    let createRequestNotificationMessage message =
        match message with
        | SendAppointments request -> request |> createAppointmentsMessage
        | SendConfirmations request -> request |> createConfirmationsMessage

    let send ct data =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> ResultAsync.wrap (data |> Web.Telegram.Client.send ct)

module private Receiver =
    open Web.Telegram.Domain.Receive

    let private receiveText ct (msg: Message<string>) client =
        async {
            match msg.Value with
            | "/start" -> return Ok(Sender.createEmbassiesButtons msg.ChatId)
            | HasPayload(embassy, country, city, payload) ->
                let! message = Sender.createPayloadResponseMessage ct msg.ChatId (embassy, country, city) payload
                return Ok message
            | _ -> return Error <| NotSupported $"Message text: {msg.Value}"
        }

    let private receiveCallback ct msg client =
        async {
            match msg.Value with
            | HasEmbassy value -> return Ok(Sender.createCountriesButtons msg.Id msg.ChatId value)
            | HasCountry value -> return Ok(Sender.createCitiesButtons msg.Id msg.ChatId value)
            | HasCity value -> return Ok(Sender.createPayloadRequestMessage msg.Id msg.ChatId value)
            | _ -> return Error <| NotSupported $"Callback data: {msg.Value}"
        }
        |> ResultAsync.bind' (fun data -> client |> Web.Telegram.Client.send ct data)

    let private receiveMessage ct msg client =
        match msg with
        | Text text -> client |> receiveText ct text
        | _ -> $"Message type: {msg}" |> NotSupported |> Error |> async.Return
        |> ResultAsync.bind' (fun data -> client |> Web.Telegram.Client.send ct data)

    let receive ct client =
        fun data ->
            match data with
            | Message msg -> client |> receiveMessage ct msg
            | CallbackQuery msg -> client |> receiveCallback ct msg
            | _ -> $"Data type: {data}" |> NotSupported |> Error |> async.Return
            |> ResultAsync.map (fun _ -> ())

module private Listener =

    let private createListener ct =
        Result.map (fun client -> Web.Domain.Listener.Telegram(client, Receiver.receive ct client))

    let listen ct =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> createListener ct
        |> Web.Client.listen ct

let sendNotification ct notification =
    notification
    |> Sender.createRequestNotificationMessage
    |> Option.map (Sender.send ct)

let listen = Listener.listen
