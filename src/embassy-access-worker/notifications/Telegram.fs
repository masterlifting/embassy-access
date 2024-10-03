module EmbassyAccess.Worker.Notifications.Telegram

open System
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open EmbassyAccess.Worker
open Infrastructure
open Web.Telegram.Domain
open EmbassyAccess

let private AdminChatId = 379444553L

[<Literal>]
let private EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN = "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"

module private Data =
    open Web.Telegram.Domain.Send
    open Persistence.Domain

    let create<'a> (chatId, msgId) (value: 'a) =
        { Id = msgId
          ChatId = chatId
          Value = value }

    module Create =

        module Message =

            let error chatId (error: Error') =
                error.Message |> create (chatId, New) |> Text |> Some

            let confirmation chatId (requestId, embassy, (confirmations: Confirmation list)) =
                confirmations
                |> Seq.map _.Description
                |> String.concat "\n"
                |> create (chatId, New)
                |> Text

            let payloadRequest (chatId, msgId) (embassy', country', city') =
                match (embassy', country', city') |> Mapper.Embassy.create with
                | Error error -> error.Message
                | Ok embassy ->
                    match embassy with
                    | Russian _ ->
                        $"Send your payload using the following format: '{embassy'}|{country'}|{city'}|your_link_here'."
                    | _ -> $"Not supported embassy: '{embassy'}'."
                |> create (chatId, msgId |> Replace)
                |> Text

            let payloadResponse ct chatId (embassy', country', city') payload =
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

                        request
                        |> Api.validateRequest
                        |> Result.bind (fun _ -> Persistence.Storage.create InMemory)
                        |> ResultAsync.wrap (Repository.Command.Request.create ct request)
                        |> ResultAsync.map (fun request -> $"Request created for '{request.Embassy}'.")
                        |> ResultAsync.map (create (chatId, New))
                        |> ResultAsync.map Text
                    | _ -> embassy' |> NotSupported |> Error |> async.Return)

        module Buttons =
            let appointments chatId (embassy, (appointments: Set<Appointment>)) =
                { Buttons.Name = $"Found Appointments for {embassy}"
                  Columns = 3
                  Data =
                    appointments
                    |> Seq.map (fun x -> x.Value, x.Description |> Option.defaultValue "No data")
                    |> Map.ofSeq }
                |> create (chatId, New)
                |> Buttons

            let embassies chatId =
                let data =
                    Api.getEmbassies ()
                    |> Seq.concat
                    |> Seq.map Mapper.Embassy.toExternal
                    |> Seq.map (fun embassy -> embassy.Name, embassy.Name)
                    |> Seq.sortBy fst
                    |> Map

                { Buttons.Name = "Available Embassies."
                  Columns = 3
                  Data = data }
                |> create (chatId, New)
                |> Buttons

            let countries (chatId, msgId) embassy' =
                let data =
                    Api.getEmbassies ()
                    |> Seq.concat
                    |> Seq.map Mapper.Embassy.toExternal
                    |> Seq.filter (fun embassy -> embassy.Name = embassy')
                    |> Seq.map _.Country
                    |> Seq.map (fun country -> (embassy' + "|" + country.Name), country.Name)
                    |> Seq.sortBy fst
                    |> Map

                { Buttons.Name = $"Countries for '{embassy'}' embassy."
                  Columns = 3
                  Data = data }
                |> create (chatId, msgId |> Replace)
                |> Buttons

            let cities (chatId, msgId) (embassy', country') =
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

                { Buttons.Name = $"Cities for '{embassy'}' embassy in '{country'}'."
                  Columns = 3
                  Data = data }
                |> create (chatId, msgId |> Replace)
                |> Buttons

module private Producer =
    open Persistence.Domain

    let getChatId request =
        Persistence.Storage.create InMemory
        |> ResultAsync.wrap (fun storage -> AdminChatId |> Ok |> async.Return)

    let private send ct data =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> ResultAsync.wrap (data |> Web.Telegram.Client.send ct)

    module Produce =
        let notification ct =
            function
            | SendAppointments request ->
                request
                |> Core.Create.appointments
                |> Option.map (fun (embassy, appointments) ->
                    (embassy, appointments)
                    |> Data.Create.Buttons.appointments AdminChatId
                    |> send ct)
            | SendConfirmations request ->
                request
                |> Core.Create.confirmations
                |> Option.map (fun (requestId, embassy, confirmations) ->
                    (requestId, embassy, confirmations)
                    |> Data.Create.Message.confirmation AdminChatId
                    |> send ct)
            | SendError(requestId, error) -> error |> Data.Create.Message.error AdminChatId |> Option.map (send ct)

module private Consumer =

    let private respond ct client data =
        client |> Web.Telegram.Client.send ct data |> ResultAsync.map (fun _ -> ())

    let private respondError ct chatId client (error: Error') =
        error.Message
        |> Data.create (chatId, Send.MessageId.New)
        |> Send.Text
        |> respond ct client
        |> Async.map (function
            | Ok _ -> Error error
            | Error error -> Error error)

    let private respondWithError ct chatId client result =
        match result with
        | Ok data -> data |> respond ct client
        | Error error -> error |> respondError ct chatId client

    module private Consume =
        let private (|HasEmbassy|HasCountry|HasCity|HasPayload|None|) value =
            match value with
            | AP.IsString value ->
                let data = value.Split '|'

                match data.Length with
                | 1 -> HasEmbassy(data[0])
                | 2 -> HasCountry(data[0], data[1])
                | 3 -> HasCity(data[0], data[1], data[2])
                | 4 -> HasPayload(data[0], data[1], data[2], data[3])
                | _ -> None
            | _ -> None

        let text ct (msg: Receive.Message<string>) client =
            async {
                match msg.Value with
                | "/start" -> return! Data.Create.Buttons.embassies msg.ChatId |> respond ct client
                | HasPayload(embassy, country, city, payload) ->
                    return!
                        payload
                        |> Data.Create.Message.payloadResponse ct msg.ChatId (embassy, country, city)
                        |> Async.bind (respondWithError ct msg.ChatId client)
                | _ -> return Error <| NotSupported $"Text: {msg.Value}."
            }

        let callback ct (msg: Receive.Message<string>) client =
            async {
                match msg.Value with
                | HasEmbassy value ->
                    return! Data.Create.Buttons.countries (msg.ChatId, msg.Id) value |> respond ct client
                | HasCountry value -> return! Data.Create.Buttons.cities (msg.ChatId, msg.Id) value |> respond ct client
                | HasCity value ->
                    return!
                        Data.Create.Message.payloadRequest (msg.ChatId, msg.Id) value
                        |> respond ct client
                | _ -> return Error <| NotSupported $"Callback: {msg.Value}."
            }

    let private consume ct client =
        fun data ->
            match data with
            | Receive.Message msg ->
                match msg with
                | Receive.Text text -> client |> Consume.text ct text
                | _ -> $"{msg}" |> NotSupported |> Error |> async.Return
            | Receive.CallbackQuery msg -> client |> Consume.callback ct msg
            | _ -> $"Data: {data}." |> NotSupported |> Error |> async.Return

    let start ct =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> Result.map (fun client -> Web.Domain.Listener.Telegram(client, consume ct client))
        |> Web.Client.listen ct


let send = Producer.Produce.notification
let listen ct = Consumer.start ct
