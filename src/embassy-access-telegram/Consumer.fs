module EA.Telegram.Consumer

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Consumer
open EA.Telegram.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Consumer

module private Consume =
    open EA.Telegram.Endpoints.Consumer.Core
    open EA.Telegram.Handlers.Consumer

    let private produceResult chatId ct client dataRes = produceResult dataRes chatId ct client

    let private toResponse request =
        fun deps ->
            match request with
            | Request.Users value -> deps |> Users.toResponse value
            | Request.Embassies value -> deps |> Embassies.Core.toResponse value
            | Request.RussianEmbassy value -> deps |> Embassies.Russian.toResponse value

    let text value client =
        fun deps ->
            deps
            |> Request.parse value
            |> Result.map toResponse
            |> ResultAsync.wrap (fun createResponse ->
                deps
                |> createResponse
                |> produceResult deps.ChatId deps.CancellationToken client)

    let callback value client =
        fun deps ->
            deps
            |> Request.parse value
            |> Result.map toResponse
            |> ResultAsync.wrap (fun createResponse ->
                deps
                |> createResponse
                |> produceResult deps.ChatId deps.CancellationToken client)

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
            | _ -> $"Telegram '%A{msg}'" |> NotSupported |> Error |> async.Return
        | CallbackQuery dto ->
            Persistence.Dependencies.create cfg
            |> Result.bind (Core.Dependencies.create dto ct)
            |> ResultAsync.wrap (client |> Consume.callback dto.Value)
            |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
        | _ -> $"Telegram '%A{data}'" |> NotSupported |> Error |> async.Return

let start ct cfg =
    Constants.EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN
    |> Web.Telegram.Domain.Client.EnvKey
    |> Web.Telegram.Client.init
    |> Result.map (fun client -> Web.Client.Consumer.Telegram(client, client |> create cfg ct))
    |> Web.Client.consume ct
