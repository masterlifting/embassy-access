module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain
open EA.Telegram.Domain.Message

module private Respond =

    let Ok ct client data =
        client
        |> Web.Telegram.Client.Producer.produce ct data
        |> ResultAsync.map (fun _ -> ())

    let Error ct chatId client (error: Error') =
        error.Message
        |> Message.create (chatId, Producer.DtoId.New)
        |> Producer.Text
        |> Ok ct client
        |> Async.map (function
            | Ok _ -> Error error
            | Error error -> Error error)

    let Result ct chatId client result =
        match result with
        | Ok data -> data |> Ok ct client
        | Error error -> error |> Error ct chatId client

module private Consume =
    let private (|Start|Mine|None|) value =
        match value with
        | AP.IsString value ->
            match value.Substring(0, 5) with
            | "strt$" -> Start(value.Substring 5)
            | "mine$" -> Mine(value.Substring 5)
            | _ -> None
        | _ -> None

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

    let text ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | "/start" -> return! Message.Create.Buttons.embassies msg.ChatId |> Respond.Ok ct client
            | "/mine" ->
                return!
                    Message.Create.Buttons.chatEmbassies ct cfg msg.ChatId
                    |> Async.bind (Respond.Result ct msg.ChatId client)
            | HasPayload(embassy, country, city, payload) ->
                let data: PayloadResponse =
                    { Config = cfg
                      Ct = ct
                      ChatId = msg.ChatId
                      Embassy = embassy
                      Country = country
                      City = city
                      Payload = payload }

                return!
                    Message.Create.Text.payloadResponse data
                    |> Async.bind (Respond.Result ct msg.ChatId client)
            | _ -> return Error <| NotSupported $"Text: {msg.Value}."
        }

    let callback ct cfg (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | Start value ->
                match value with
                | HasEmbassy value ->
                    return!
                        Message.Create.Buttons.countries (msg.ChatId, msg.Id) value
                        |> Respond.Ok ct client
                | HasCountry value ->
                    return! Message.Create.Buttons.cities (msg.ChatId, msg.Id) value |> Respond.Ok ct client
                | HasCity value ->
                    return!
                        Message.Create.Text.payloadRequest (msg.ChatId, msg.Id) value
                        |> Respond.Ok ct client
                | None -> return Error <| NotSupported $"Callback: {msg.Value}."
            | Mine value ->
                match value with
                | HasEmbassy value ->
                    return!
                        Message.Create.Buttons.chatCountries ct cfg (msg.ChatId, msg.Id) value
                        |> Async.bind (Respond.Result ct msg.ChatId client)
                | HasCountry value ->
                    return!
                        Message.Create.Buttons.chatCities ct cfg (msg.ChatId, msg.Id) value
                        |> Async.bind (Respond.Result ct msg.ChatId client)
                | HasCity value ->
                    return!
                        Message.Create.Text.listRequests ct cfg (msg.ChatId, msg.Id) value
                        |> Async.bind (Respond.Result ct msg.ChatId client)
                | None -> return Error <| NotSupported $"Callback: {msg.Value}."
            | None -> return Error <| NotSupported $"Callback: {msg.Value}."

        }

let private handle ct cfg client =
    fun data ->
        match data with
        | Consumer.Message message ->
            match message with
            | Consumer.Text dto -> client |> Consume.text ct cfg dto
            | _ -> $"{message}" |> NotSupported |> Error |> async.Return
        | Consumer.CallbackQuery dto -> client |> Consume.callback ct cfg dto
        | _ -> $"Data: {data}." |> NotSupported |> Error |> async.Return

let start ct cfg =
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct cfg client))
    |> Web.Client.consume ct
