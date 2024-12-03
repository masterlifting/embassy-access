module EA.Telegram.Consumer

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Dependencies

module private Consume =
    open EA.Telegram.CommandHandler
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
                        Dependencies.GetEmbassies.create deps
                        |> ResultAsync.wrap (getEmbassies None)
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.GetUserEmbassies ->
                        Dependencies.GetUserEmbassies.create deps
                        |> ResultAsync.wrap (getUserEmbassies None)
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.SetService(embassyId, serviceId, payload) ->
                        Dependencies.SetService.create deps
                        |> ResultAsync.wrap (setService (embassyId, serviceId, payload))
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
                        Dependencies.GetEmbassies.create deps
                        |> ResultAsync.wrap (getEmbassies (Some embassyId))
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.GetUserEmbassy embassyId ->
                        Dependencies.GetUserEmbassies.create deps
                        |> ResultAsync.wrap (getUserEmbassies (Some embassyId))
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | Command.GetService(embassyId, serviceId) ->
                        Dependencies.GetService.create deps
                        |> ResultAsync.wrap (getService (embassyId, Some serviceId))
                        |> produceResult deps.ChatId deps.CancellationToken client
                    | _ -> value |> NotSupported |> Error |> async.Return

let private create ct cfg client =
    fun data ->
        match data with
        | Message msg ->
            match msg with
            | Text dto ->
                ConsumerDeps.create dto cfg ct
                |> ResultAsync.wrap (client |> Consume.text dto.Value)
                |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
            | _ -> $"{msg}" |> NotSupported |> Error |> async.Return
        | CallbackQuery dto ->
            ConsumerDeps.create dto cfg ct
            |> ResultAsync.wrap (client |> Consume.callback dto.Value)
            |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
        | _ -> $"{data}" |> NotSupported |> Error |> async.Return

let start ct cfg =
    Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Domain.EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Consumer.Telegram(client, create ct cfg client))
    |> Web.Client.consume ct
