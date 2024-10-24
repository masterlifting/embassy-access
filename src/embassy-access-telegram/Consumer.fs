module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Responses

module private Consume =
    let text (msg: Dto<string>) cfg ct client =
        match msg.Value |> Command.tryFind with
        | None -> msg.Value |> NotSupported |> Error |> async.Return
        | Some cmd ->
            match cmd with
            | Command.Start -> Buttons.Create.embassies msg.ChatId |> Response.Ok client ct
            | Command.Mine ->
                Buttons.Create.userEmbassies msg.ChatId cfg ct
                |> Response.Result msg.ChatId client ct
            | Command.Subscribe(embassy, country, city, payload) ->
                Text.Create.subscribe (embassy, country, city, payload) msg.ChatId cfg ct
                |> Response.Result msg.ChatId client ct
            | _ -> msg.Value |> NotSupported |> Error |> async.Return

    let callback (msg: Dto<string>) cfg ct client =
        match msg.Value |> Command.tryFind with
        | None -> msg.Value |> NotSupported |> Error |> async.Return
        | Some cmd ->
            match cmd with
            | Command.Countries embassy ->
                Buttons.Create.countries embassy (msg.ChatId, msg.Id) |> Response.Ok client ct
            | Command.Cities(embassy, country) ->
                Buttons.Create.cities (embassy, country) (msg.ChatId, msg.Id)
                |> Response.Ok client ct
            | Command.UserCountries embassy ->
                Buttons.Create.userCountries embassy (msg.ChatId, msg.Id) cfg ct
                |> Response.Result msg.ChatId client ct
            | Command.UserCities(embassy, country) ->
                Buttons.Create.userCities (embassy, country) (msg.ChatId, msg.Id) cfg ct
                |> Response.Result msg.ChatId client ct
            | Command.SubscriptionRequest(embassy, country, city) ->
                Text.Create.subscriptionRequest (embassy, country, city) (msg.ChatId, msg.Id)
                |> Response.Ok client ct
            | Command.UserSubscriptions(embassy, country, city) ->
                Text.Create.userSubscriptions (embassy, country, city) (msg.ChatId, msg.Id) cfg ct
                |> Response.Result msg.ChatId client ct
            | Command.ConfirmAppointment(embassy, country, city, payload) ->
                Text.Create.confirmAppointment (embassy, country, city, payload) msg.ChatId cfg ct
                |> Response.Result msg.ChatId client ct
            | _ -> msg.Value |> NotSupported |> Error |> async.Return

let private handle ct cfg client =
    fun data ->
        match data with
        | Message msg ->
            match msg with
            | Text dto ->
                client
                |> Consume.text dto cfg ct
                |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
            | _ -> $"{msg}" |> NotSupported |> Error |> async.Return
        | CallbackQuery dto ->
            client
            |> Consume.callback dto cfg ct
            |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
        | _ -> $"{data}" |> NotSupported |> Error |> async.Return

let start ct cfg =
    Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Domain.EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct cfg client))
    |> Web.Client.consume ct
