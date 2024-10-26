module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain

module private Consume =
    let text (msg: Dto<string>) cfg ct client =
        match msg.Value |> Command.tryFind with
        | None -> msg.Value |> NotSupported |> Error |> async.Return
        | Some cmd ->
            match cmd with
            | Command.Start -> Command.Create.start msg.ChatId |> Response.Ok client ct
            | Command.Mine -> Command.Create.mine msg.ChatId cfg ct |> Response.Result msg.ChatId client ct
            | Command.Subscribe(embassy, payload) ->
                Command.Create.subscribe (embassy, payload) msg.ChatId cfg ct
                |> Response.Result msg.ChatId client ct
            | _ -> msg.Value |> NotSupported |> Error |> async.Return

    let callback (msg: Dto<string>) cfg ct client =
        match msg.Value |> Command.tryFind with
        | None -> msg.Value |> NotSupported |> Error |> async.Return
        | Some cmd ->
            match cmd with
            | Command.Countries embassy ->
                Command.Create.countries embassy (msg.ChatId, msg.Id) |> Response.Ok client ct
            | Command.Cities(embassy, country) ->
                Command.Create.cities (embassy, country) (msg.ChatId, msg.Id)
                |> Response.Ok client ct
            | Command.UserCountries embassy ->
                Command.Create.userCountries embassy (msg.ChatId, msg.Id) cfg ct
                |> Response.Result msg.ChatId client ct
            | Command.UserCities(embassy, country) ->
                Command.Create.userCities (embassy, country) (msg.ChatId, msg.Id) cfg ct
                |> Response.Result msg.ChatId client ct
            | Command.SubscriptionRequest embassy ->
                Command.Create.subscriptionRequest embassy (msg.ChatId, msg.Id)
                |> Response.Ok client ct
            | Command.UserSubscriptions embassy ->
                Command.Create.userSubscriptions embassy (msg.ChatId, msg.Id) cfg ct
                |> Response.Result msg.ChatId client ct
            | Command.ConfirmAppointment(embassy, appointment) ->
                Command.Create.confirmAppointment (embassy, appointment) msg.ChatId cfg ct
                |> Response.Result msg.ChatId client ct
            | _ -> msg.Value |> NotSupported |> Error |> async.Return

let private Create ct cfg client =
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
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, Create ct cfg client))
    |> Web.Client.consume ct
