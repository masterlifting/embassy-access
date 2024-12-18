module EA.Telegram.Consumer

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Handlers.Consumer

module private Consume =

    let private produceResult chatId ct client dataRes = produceResult dataRes chatId ct client

    let text value client =
        fun deps ->
            match value |> Command.get with
            | Error error -> error |> Error |> async.Return
            | Ok cmd ->
                match cmd with
                | None -> value |> NotSupported |> Error |> async.Return
                | Some cmd ->
                    match cmd with
                    | Command.GetEmbassies ->
                        Core.getEmbassies None deps
                        |> (client |> produceResult deps.ChatId deps.CancellationToken)
                    | Command.GetUserEmbassies ->
                        Core.getUserEmbassies None deps
                        |> (client |> produceResult deps.ChatId deps.CancellationToken)
                    | Command.SetService(embassyId, serviceId, payload) ->
                        Core.setService (embassyId, serviceId, payload) deps
                        |> (client |> produceResult deps.ChatId deps.CancellationToken)
                    | _ -> value |> NotSupported |> Error |> async.Return

    let callback value client =
        fun deps ->
            match value |> Command.get with
            | Error error -> error |> Error |> async.Return
            | Ok cmd ->
                match cmd with
                | None -> value |> NotSupported |> Error |> async.Return
                | Some cmd ->
                    match cmd with
                    | Command.GetEmbassy embassyId ->
                        Core.getEmbassies (Some embassyId) deps
                        |> (client |> produceResult deps.ChatId deps.CancellationToken)
                    | Command.GetUserEmbassy embassyId ->
                        Core.getUserEmbassies (Some embassyId) deps
                        |> (client |> produceResult deps.ChatId deps.CancellationToken)
                    | Command.GetService(embassyId, serviceId) ->
                        Core.getService (embassyId, Some serviceId) deps
                        |> (client |> produceResult deps.ChatId deps.CancellationToken)
                    | Command.SetService(embassyId, serviceId, payload) ->
                        Core.setService (embassyId, serviceId, payload) deps
                        |> (client |> produceResult deps.ChatId deps.CancellationToken)
                    | _ -> value |> NotSupported |> Error |> async.Return

let private create cfg ct client =
    fun data ->
        match data with
        | Message msg ->
            match msg with
            | Text dto ->
                Persistence.Dependencies.create cfg
                |> Result.bind (Core.Dependencies.create dto ct)
                |> ResultAsync.wrap (client |> Consume.text dto.Value)
                |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
            | _ -> $"{msg}" |> NotSupported |> Error |> async.Return
        | CallbackQuery dto ->
            Persistence.Dependencies.create cfg
            |> Result.bind (Core.Dependencies.create dto ct)
            |> ResultAsync.wrap (client |> Consume.callback dto.Value)
            |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
        | _ -> $"{data}" |> NotSupported |> Error |> async.Return

let start ct cfg =
    Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Domain.Client.EnvKey
    |> Web.Telegram.Client.init
    |> Result.map (fun client -> Web.Client.Consumer.Telegram(client, client |> create cfg ct))
    |> Web.Client.consume ct
