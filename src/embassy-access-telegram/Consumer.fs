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
                | Command.GetEmbassies ->
                    CommandHandler.Common.embassies None (cfg, msg.ChatId, msg.Id)
                    |> produceOk client ct
                | _ -> msg.Value |> NotSupported |> Error |> async.Return

    let callback (msg: Dto<string>) cfg ct client =
        match msg.Value |> Command.get with
        | Error error -> error |> Error |> async.Return
        | Ok cmd ->
            match cmd with
            | None -> msg.Value |> NotSupported |> Error |> async.Return
            | Some cmd ->
                match cmd with
                | Command.GetEmbassy embassyId ->
                    CommandHandler.Common.embassies (Some embassyId) (cfg, msg.ChatId, msg.Id)
                    |> produceOk client ct
                | Command.GetService(embassyId, serviceId) ->
                    CommandHandler.Common.getService (embassyId, Some serviceId) (cfg, msg.ChatId, msg.Id)
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
    Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Domain.EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, create ct cfg client))
    |> Web.Client.consume ct
