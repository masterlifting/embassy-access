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

    let send ct data =
        EnvKey EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
        |> Web.Telegram.Client.create
        |> ResultAsync.wrap (data |> Web.Telegram.Client.send ct)

    module Data =
        module Message =

            let confirmation request =
                Create.confirmationsNotification request
                |> Option.map (fun (requestId, embassy, confirmations) ->
                    confirmations |> Seq.map _.Description |> String.concat "\n")

            let payloadRequest (embassy', country', city') =
                match (embassy', country', city') |> Mapper.Embassy.create with
                | Error error -> error.Message
                | Ok embassy ->
                    match embassy with
                    | Russian _ ->
                        $"Send your payload using the following format: '{embassy'}|{country'}|{city'}|your_link_here'."
                    | _ -> $"Not supported embassy: '{embassy'}'."

            let payloadResponse ct (embassy', country', city') payload =
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
                        |> ResultAsync.map (fun request -> $"Request created for '{request.Embassy}'.")
                    | _ -> embassy' |> NotSupported |> Error |> async.Return)

        module Buttons =
            let appointments request =
                Create.appointmentsNotification request
                |> Option.map (fun (embassy, appointments) ->
                    { Buttons.Name = $"Found Appointments for {embassy}"
                      Columns = 3
                      Data =
                        appointments
                        |> Seq.map (fun x -> x.Value, x.Description |> Option.defaultValue "No data")
                        |> Map.ofSeq })

            let embassies () =
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

            let countries embassy' =
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

            let cities (embassy', country') =
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

    module Response =

        let create<'a> (msgId, chatId) (value: 'a) =
            { Id = msgId
              ChatId = chatId
              Value = value }

        let notification chatId message =
            match message with
            | SendAppointments request ->
                request
                |> Data.Buttons.appointments
                |> Option.map (create (New, chatId))
                |> Option.map Buttons
            | SendConfirmations request ->
                request
                |> Data.Message.confirmation
                |> Option.map (create (New, chatId))
                |> Option.map Text

module private Receiver =
    let private receiveText ct (msg: Receive.Message<string>) client =
        async {
            match msg.Value with
            | "/start" ->
                return
                    Sender.Data.Buttons.embassies ()
                    |> Sender.Response.create (Send.New, msg.ChatId)
                    |> Send.Buttons
                    |> Ok
            | HasPayload(embassy, country, city, payload) ->
                return!
                    payload
                    |> Sender.Data.Message.payloadResponse ct (embassy, country, city)
                    |> ResultAsync.map (Sender.Response.create (Send.New, msg.ChatId))
                    |> ResultAsync.map Send.Text
            | _ -> return Error <| NotSupported $"Message text: {msg.Value}"
        }

    let private receiveCallback ct (msg: Receive.Message<string>) client =
        async {
            match msg.Value with
            | HasEmbassy value ->
                return
                    Sender.Data.Buttons.countries value
                    |> Sender.Response.create (msg.Id |> Send.Replace, msg.ChatId)
                    |> Send.Buttons
                    |> Ok
            | HasCountry value ->
                return
                    Sender.Data.Buttons.cities value
                    |> Sender.Response.create (msg.Id |> Send.Replace, msg.ChatId)
                    |> Send.Buttons
                    |> Ok
            | HasCity value ->
                return
                    Sender.Data.Message.payloadRequest value
                    |> Sender.Response.create (msg.Id |> Send.Replace, msg.ChatId)
                    |> Send.Text
                    |> Ok
            | _ -> return Error <| NotSupported $"Callback data: {msg.Value}"
        }
        |> ResultAsync.bind' (fun data -> client |> Web.Telegram.Client.send ct data)

    let private receiveMessage ct msg client =
        match msg with
        | Receive.Text text -> client |> receiveText ct text
        | _ -> $"Message type: {msg}" |> NotSupported |> Error |> async.Return
        |> ResultAsync.bind' (fun data -> client |> Web.Telegram.Client.send ct data)

    let receive ct client =
        fun data ->
            match data with
            | Receive.Message msg -> client |> receiveMessage ct msg
            | Receive.CallbackQuery msg -> client |> receiveCallback ct msg
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
    |> Sender.Response.notification AdminChatId
    |> Option.map (Sender.send ct)

let listen = Listener.listen
