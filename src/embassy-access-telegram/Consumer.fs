﻿module EA.Telegram.Consumer

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Dependencies

module private Consume =
    open EA.Telegram.CommandHandler.Core

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
                        getEmbassies None deps
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.GetUserEmbassies ->
                        getUserEmbassies None deps
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.SetService(embassyId, serviceId, payload) ->
                        setService (embassyId, serviceId, payload) deps
                        |> produceResult deps.ChatId deps.CancellationToken client
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
                        getEmbassies (Some embassyId) deps
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.GetUserEmbassy embassyId ->
                        getUserEmbassies (Some embassyId) deps
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.GetService(embassyId, serviceId) ->
                        getService (embassyId, Some serviceId) deps
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | _ -> value |> NotSupported |> Error |> async.Return

let private create ct cfg client =
    fun data ->
        match data with
        | Message msg ->
            match msg with
            | Text dto ->
                Persistence.Dependencies.create cfg
                |> Result.map (Consumer.Dependencies.create dto cfg ct)
                |> Result.bind CommandHandler.Core.Dependencies.create
                |> ResultAsync.wrap (client |> Consume.text dto.Value)
                |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
            | _ -> $"{msg}" |> NotSupported |> Error |> async.Return
        | CallbackQuery dto ->
            Persistence.Dependencies.create cfg
            |> Result.map (Consumer.Dependencies.create dto cfg ct)
            |> Result.bind CommandHandler.Core.Dependencies.create
            |> ResultAsync.wrap (client |> Consume.callback dto.Value)
            |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
        | _ -> $"{data}" |> NotSupported |> Error |> async.Return

let start ct cfg =
    Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Domain.Client.EnvKey
    |> Web.Telegram.Client.init
    |> Result.map (fun client -> Web.Client.Consumer.Telegram(client, create ct cfg client))
    |> Web.Client.consume ct
