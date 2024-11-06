module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain

module private Consume =
    let text (msg: Dto<string>) cfg ct client =
        match msg.Value |> Command.get with
        | Error error -> error |> Error |> async.Return
        | Ok cmd ->
            match cmd with
            | None -> msg.Value |> NotSupported |> Error |> async.Return
            | Some cmd ->
                match cmd with
                | Command.Start -> CommandHandler.start msg.ChatId |> produceOk client ct
                | Command.Mine -> CommandHandler.mine msg.ChatId cfg ct |> produceResult msg.ChatId client ct
                | Command.SubscribeSearchAppointments(embassy, payload) ->
                    CommandHandler.subscribe (embassy, payload, "searchappointments") msg.ChatId cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.SubscribeSearchOthers(embassy, payload) ->
                    CommandHandler.subscribe (embassy, payload, "searchothers") msg.ChatId cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.SubscribeSearchPassportResult(embassy, payload) ->
                    CommandHandler.subscribe (embassy, payload, "searchpassportresult") msg.ChatId cfg ct
                    |> produceResult msg.ChatId client ct
                | _ -> msg.Value |> NotSupported |> Error |> async.Return

    let callback (msg: Dto<string>) cfg ct client =
        match msg.Value |> Command.get with
        | Error error -> error |> Error |> async.Return
        | Ok cmd ->
            match cmd with
            | None -> msg.Value |> NotSupported |> Error |> async.Return
            | Some cmd ->
                match cmd with
                | Command.Countries embassy ->
                    CommandHandler.countries embassy (msg.ChatId, msg.Id) |> produceOk client ct
                | Command.Cities(embassy, country) ->
                    CommandHandler.cities (embassy, country) (msg.ChatId, msg.Id)
                    |> produceOk client ct
                | Command.UserCountries embassy ->
                    CommandHandler.userCountries embassy (msg.ChatId, msg.Id) cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.UserCities(embassy, country) ->
                    CommandHandler.userCities (embassy, country) (msg.ChatId, msg.Id) cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.SubscriptionRequest embassy ->
                    CommandHandler.subscriptionRequest embassy (msg.ChatId, msg.Id)
                    |> produceOk client ct
                | Command.UserSubscriptions embassy ->
                    CommandHandler.userSubscriptions embassy (msg.ChatId, msg.Id) cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.ChooseAppointmentRequest(embassy, appointmentId) ->
                    CommandHandler.chooseAppointmentRequest (embassy, appointmentId) msg.ChatId cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.ConfirmAppointment(requestId, appointmentId) ->
                    CommandHandler.confirmAppointment (requestId, appointmentId) msg.ChatId cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.RemoveSubscription subscriptionId ->
                    CommandHandler.removeSubscription subscriptionId msg.ChatId cfg ct
                    |> produceResult msg.ChatId client ct
                | Command.ChoseSubscriptionRequest(embassy, command) ->
                    CommandHandler.choseSubscriptionRequest (embassy, command) (msg.ChatId, msg.Id)
                    |> produceResult msg.ChatId client ct
                | _ -> msg.Value |> NotSupported |> Error |> async.Return

let private create ct cfg client =
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
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, create ct cfg client))
    |> Web.Client.consume ct
