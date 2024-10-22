module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Domain.Response
open EA.Telegram.Responses

module private Router =
    let text (value: string) =
        let s = value |> Key.unwrap

        match s.Length with
        | 0 -> NoText
        | 1 ->
            match s[0] with
            | "/start" -> Embassies(Buttons.Create.embassies ())
            | "/mine" -> UserEmbassies(Buttons.Create.userEmbassies ())
            | _ -> NoText
        | _ ->
            let length = s.Length - 1

            match s[0] with
            | Key.SUB ->
                match length with
                | 4 -> Subscribe(Text.Create.subscribe (s[1], s[2], s[3], s[4]))
                | _ -> NoText
            | _ -> NoText

    let callback (value: string) =
        let s = value |> Key.unwrap

        match s.Length with
        | 0 -> NoCallback
        | 1 -> NoCallback
        | _ ->
            let length = s.Length - 1

            match s[0] with
            | Key.SUB ->
                match length with
                | 1 -> Countries(Buttons.Create.countries s[1])
                | 2 -> Cities(Buttons.Create.cities (s[1], s[2]))
                | 3 -> SubscriptionRequest(Text.Create.subscriptionRequest (s[1], s[2], s[3]))
                | _ -> NoCallback
            | Key.INF ->
                match length with
                | 1 -> UserCountries(Buttons.Create.userCountries s[1])
                | 2 -> UserCities(Buttons.Create.userCities (s[1], s[2]))
                | 3 -> UserSubscriptions(Text.Create.userSubscriptions (s[1], s[2], s[3]))
                | _ -> NoCallback
            | Key.APT ->
                match length with
                | 4 -> ConfirmAppointment(Text.Create.confirmAppointment (s[1], s[2], s[3], s[4]))
                | _ -> NoCallback
            | _ -> NoCallback
            
    let callback' (value: string) =
        let payload = value |> Key.unwrap'

        match payload.Route with
        | "countries/get" ->
            let  data = payload.Data
            match data.["embassy"] |> Json.deserialize<EA.Domain.External.Embassy> with
            | Ok embassy -> Countries(Buttons.Create.countries embassy.Name)
            | Error _ -> NoCallback
        | _ -> NoCallback

module private Consume =
    let text (msg: Dto<string>) cfg ct client =
        match msg.Value |> Router.text with
        | Embassies cmd -> cmd msg.ChatId |> Response.Ok client ct
        | Subscribe cmd -> cmd msg.ChatId cfg ct |> Response.Result msg.ChatId client ct
        | UserEmbassies cmd -> cmd msg.ChatId cfg ct |> Response.Result msg.ChatId client ct
        | NoText
        | _ -> msg.Value |> NotSupported |> Error |> async.Return

    let callback (msg: Dto<string>) cfg ct client =
        match msg.Value |> Router.callback' with
        | Countries cmd -> cmd (msg.ChatId, msg.Id) |> Response.Ok client ct
        | Cities cmd -> cmd (msg.ChatId, msg.Id) |> Response.Ok client ct
        | UserCountries cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Response.Result msg.ChatId client ct
        | UserCities cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Response.Result msg.ChatId client ct
        | SubscriptionRequest cmd -> cmd (msg.ChatId, msg.Id) |> Response.Ok client ct
        | UserSubscriptions cmd -> cmd (msg.ChatId, msg.Id) cfg ct |> Response.Result msg.ChatId client ct
        | NoCallback
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
    Domain.Key.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Domain.EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, handle ct cfg client))
    |> Web.Client.consume ct
