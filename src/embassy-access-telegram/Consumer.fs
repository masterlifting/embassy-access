﻿module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain
open EA.Telegram.Domain.Message

module private Respond =

    module Message =

        let Ok ct client data =
            client
            |> Web.Telegram.Client.Producer.produce ct data
            |> ResultAsync.map (fun _ -> ())

        let Error ct chatId (error: Error') client =
            error.Message
            |> Message.create (chatId, Producer.DtoId.New)
            |> Producer.Text
            |> Ok ct client
            |> Async.map (function
                | Ok _ -> Error error
                | Error error -> Error error)

        let Result ct chatId client =
            Async.bind (function
                | Ok data -> data |> Ok ct client
                | Error error -> client |> Error ct chatId error)

    let text (value: string) =

        let data = value.Split '|'

        match data.Length with
        | 0 -> NoText
        | 1 ->
            match data[0] with
            | "/start" -> SupportedEmbassies(Message.Create.Buttons.embassies ())
            | "/mine" -> UserEmbassies(Message.Create.Buttons.userEmbassies ())
            | _ -> NoText
        | _ ->
            match data[0] with
            | "SUBSCRIBE" ->
                match data.Length - 1 with
                | 4 ->
                    let embassy = data[1]
                    let country = data[2]
                    let city = data[3]
                    let payload = data[4]
                    let cmd = Message.Create.Text.subscribe (embassy, country, city, payload)
                    SubscriptionResult cmd
                | _ -> NoText
            | "INFO" ->
                match data.Length - 1 with
                | 3 ->
                    let embassy = data.[1]
                    let country = data.[2]
                    let city = data.[3]
                    let cmd = Message.Create.Text.userRequests (embassy, country, city)
                    UserSubscriptions cmd
                | _ -> NoText
            | _ -> NoText

    let callback (value: string) =
        let data = value.Split '|'

        match data.Length with
        | 0 -> NoCallback
        | 1 -> NoCallback
        | _ ->
            match data[0] with
            | "SUBSCRIBE" ->
                match data.Length - 1 with
                | 1 ->
                    let embassy = data[1]
                    let cmd = Message.Create.Buttons.countries embassy
                    SupportedCountries cmd
                | 2 ->
                    let embassy = data[1]
                    let country = data[2]
                    let cmd = Message.Create.Buttons.cities (embassy, country)
                    SupportedCities cmd
                | _ -> NoCallback
            | "INFO" ->
                match data.Length - 1 with
                | 1 ->
                    let embassy = data[1]
                    let cmd = Message.Create.Buttons.userCountries embassy
                    UserCountries cmd
                | 2 ->
                    let embassy = data[1]
                    let country = data[2]
                    let cmd = Message.Create.Buttons.userCities (embassy, country)
                    UserCities cmd
                | _ -> NoCallback
            | _ -> NoCallback

module private Consume =
    open Respond

    let text ct cfg (msg: Consumer.Dto<string>) client =
        match msg.Value |> text with
        | SupportedEmbassies cmd -> cmd msg.ChatId |> Message.Ok ct client
        | UserEmbassies cmd -> cmd msg.ChatId cfg ct |> Message.Result ct msg.ChatId client
        | SubscriptionResult cmd -> cmd msg.ChatId cfg ct |> Message.Result ct msg.ChatId client
        | UserSubscriptions cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Message.Result ct msg.ChatId client
        | NoText
        | _ -> msg.Value |> NotSupported |> Error |> async.Return

    let callback ct cfg (msg: Consumer.Dto<string>) client =
        match msg.Value |> callback with
        | SupportedCountries cmd -> cmd (msg.ChatId, msg.Id) |> Message.Ok ct client
        | SupportedCities cmd -> cmd (msg.ChatId, msg.Id) |> Message.Ok ct client
        | UserCountries cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Message.Result ct msg.ChatId client
        | UserCities cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Message.Result ct msg.ChatId client
        | NoCallback
        | _ -> msg.Value |> NotSupported |> Error |> async.Return

let private handle ct cfg client =
    fun data ->
        match data with
        | Consumer.Message message ->
            match message with
            | Consumer.Text dto -> client |> Consume.text ct cfg dto
            | _ -> $"%A{message}" |> NotSupported |> Error |> async.Return
        | Consumer.CallbackQuery dto -> client |> Consume.callback ct cfg dto
        | _ -> $"%A{data}." |> NotSupported |> Error |> async.Return

let start ct cfg =
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct cfg client))
    |> Web.Client.consume ct
