module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Responses

let private command value =
    match value |> Command.tryFind with
    | None -> None
    | Some command ->
        match command with
        | Command.Start -> Some <| Embassies(Buttons.Create.embassies ())
        | Command.Mine -> Some <| UserEmbassies(Buttons.Create.userEmbassies ())
        | Command.Countries embassy -> Some <| Countries(Buttons.Create.countries embassy)
        | Command.Cities(embassy, country) -> Some <| Cities(Buttons.Create.cities (embassy, country))
        | Command.UserCountries embassy -> Some <| UserCountries(Buttons.Create.userCountries embassy)
        | Command.UserCities(embassy, country) -> Some <| UserCities(Buttons.Create.userCities (embassy, country))
        | Command.UserSubscriptions(embassy, country, city) ->
            Some
            <| UserSubscriptions(Text.Create.userSubscriptions (embassy, country, city))
        | Command.Subscribe(embassy, country, city, payload) ->
            Some <| Subscribe(Text.Create.subscribe (embassy, country, city, payload))
        | Command.SubscriptionRequest(embassy, country, city) ->
            Some
            <| SubscriptionRequest(Text.Create.subscriptionRequest (embassy, country, city))
        | Command.ConfirmAppointment(embassy, country, city, payload) ->
            Some
            <| ConfirmAppointment(Text.Create.confirmAppointment (embassy, country, city, payload))

module private Consume =
    let text (msg: Dto<string>) cfg ct client =
        match msg.Value |> command with
        | None -> msg.Value |> NotSupported |> Error |> async.Return
        | Some cmd ->
            match cmd with
            | Embassies cmd -> cmd msg.ChatId |> Response.Ok client ct
            | Subscribe cmd -> cmd msg.ChatId cfg ct |> Response.Result msg.ChatId client ct
            | UserEmbassies cmd -> cmd msg.ChatId cfg ct |> Response.Result msg.ChatId client ct
            | _ -> msg.Value |> NotSupported |> Error |> async.Return

    let callback (msg: Dto<string>) cfg ct client =
        match msg.Value |> command with
        | None -> msg.Value |> NotSupported |> Error |> async.Return
        | Some cmd ->
            match cmd with
            | Countries cmd -> cmd (msg.ChatId, msg.Id) |> Response.Ok client ct
            | Cities cmd -> cmd (msg.ChatId, msg.Id) |> Response.Ok client ct
            | UserCountries cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Response.Result msg.ChatId client ct
            | UserCities cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Response.Result msg.ChatId client ct
            | SubscriptionRequest cmd -> cmd (msg.ChatId, msg.Id) |> Response.Ok client ct
            | UserSubscriptions cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Response.Result msg.ChatId client ct
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
