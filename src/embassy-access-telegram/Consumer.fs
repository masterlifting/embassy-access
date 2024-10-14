﻿module EA.Telegram.Consumer

open System
open Infrastructure
open Persistence.Domain
open Web.Telegram.Domain

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

    let text ct pcs (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | "/start" -> return! Message.Create.Buttons.embassies msg.ChatId |> Respond.Ok ct client
            | HasPayload(embassy, country, city, payload) ->
                return!
                    payload
                    |> Message.Create.Text.payloadResponse ct pcs msg.ChatId (embassy, country, city)
                    |> Async.bind (Respond.Result ct msg.ChatId client)
            | _ -> return Error <| NotSupported $"Text: {msg.Value}."
        }

    let callback ct (msg: Consumer.Dto<string>) client =
        async {
            match msg.Value with
            | HasEmbassy value ->
                return! Message.Create.Buttons.countries (msg.ChatId, msg.Id) value |> Respond.Ok ct client
            | HasCountry value -> return! Message.Create.Buttons.cities (msg.ChatId, msg.Id) value |> Respond.Ok ct client
            | HasCity value ->
                return!
                    Message.Create.Text.payloadRequest (msg.ChatId, msg.Id) value
                    |> Respond.Ok ct client
            | _ -> return Error <| NotSupported $"Callback: {msg.Value}."
        }

let private handle ct client configuration =
    fun data ->
        match data with
        | Consumer.Message message ->
            match message with
            | Consumer.Text dto ->
                configuration
                |> Persistence.Storage.getConnectionString FileSystem.SectionName
                |> ResultAsync.wrap (fun pcs -> client |> Consume.text ct pcs dto)
            | _ -> $"{message}" |> NotSupported |> Error |> async.Return
        | Consumer.CallbackQuery dto -> client |> Consume.callback ct dto
        | _ -> $"Data: {data}." |> NotSupported |> Error |> async.Return

let start ct configuration =
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct client configuration))
    |> Web.Client.consume ct
