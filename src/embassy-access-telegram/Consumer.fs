module EmbassyAccess.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain

let private respond ct client data =
    client
    |> Web.Telegram.Client.Producer.produce ct data
    |> ResultAsync.map (fun _ -> ())

let private respondError ct chatId client (error: Error') =
    error.Message
    |> Message.create (chatId, Producer.DtoId.New)
    |> Producer.Text
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

    let text ct (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | "/start" -> return! Message.Create.Buttons.embassies msg.ChatId |> respond ct client
            | HasPayload(embassy, country, city, payload) ->
                return!
                    payload
                    |> Message.Create.Text.payloadResponse ct msg.ChatId (embassy, country, city)
                    |> Async.bind (respondWithError ct msg.ChatId client)
            | _ -> return Error <| NotSupported $"Text: {msg.Value}."
        }

    let callback ct (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | HasEmbassy value ->
                return! Message.Create.Buttons.countries (msg.ChatId, msg.Id) value |> respond ct client
            | HasCountry value -> return! Message.Create.Buttons.cities (msg.ChatId, msg.Id) value |> respond ct client
            | HasCity value ->
                return!
                    Message.Create.Text.payloadRequest (msg.ChatId, msg.Id) value
                    |> respond ct client
            | _ -> return Error <| NotSupported $"Callback: {msg.Value}."
        }

let private handle ct client configuration =
    fun data ->
        match data with
        | Consumer.Message message ->
            match message with
            | Consumer.Text dto -> client |> Consume.text ct dto
            | _ -> $"{message}" |> NotSupported |> Error |> async.Return
        | Consumer.CallbackQuery dto -> client |> Consume.callback ct dto
        | _ -> $"Data: {data}." |> NotSupported |> Error |> async.Return

let start ct configuration =
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct client configuration))
    |> Web.Client.consume ct
