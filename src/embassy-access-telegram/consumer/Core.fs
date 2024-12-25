module EA.Telegram.Consumer.Core

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Consumer.Dependencies

module private Consume =
    open EA.Telegram.Consumer.Endpoints
    open EA.Telegram.Consumer.Handlers

    let private produceResult chatId ct client dataRes = produceResult dataRes chatId ct client

    let private toResponse request =
        fun deps ->
            match request with
            | Core.Request.Embassies value -> deps |> Embassies.toResponse value
            | Core.Request.Users value -> deps |> Users.toResponse value
            | Core.Request.RussianEmbassy value -> deps |> RussianEmbassy.toResponse value

    let text value client =
        fun deps ->
            deps
            |> Core.Request.parse value
            |> Result.map toResponse
            |> ResultAsync.wrap (fun createResponse ->
                deps |> createResponse |> produceResult deps.ChatId deps.CancellationToken client)

    let callback value client =
        fun deps ->
            deps
            |> Core.Request.parse value
            |> Result.map toResponse
            |> ResultAsync.wrap (fun createResponse ->
                deps |> createResponse |> produceResult deps.ChatId deps.CancellationToken client)

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
