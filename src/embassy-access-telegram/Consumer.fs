module EA.Telegram.Consumer

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Consumer
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Consumer

module private Consume =
    open EA.Telegram.Controllers.Consumer
    open EA.Telegram.Endpoints.Consumer.Request

    let private produceResult chatId ct client dataRes = produceResult dataRes chatId ct client

    let text value client =
        fun deps ->
            deps
            |> Route.parse value
            |> Result.map Consumer.respond
            |> ResultAsync.wrap (fun createResponse ->
                deps
                |> createResponse
                |> produceResult deps.ChatId deps.CancellationToken client)

    let callback value client =
        fun deps ->
            deps
            |> Route.parse value
            |> Result.map Consumer.respond
            |> ResultAsync.wrap (fun createResponse ->
                deps
                |> createResponse
                |> produceResult deps.ChatId deps.CancellationToken client)

let consume data =
    fun client cfg ct ->
        match data with
        | Message msg ->
            match msg with
            | Text dto ->
                Persistence.Dependencies.create cfg
                |> Result.bind (Consumer.Dependencies.create dto ct)
                |> ResultAsync.wrap (client |> Consume.text dto.Value)
                |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
            | _ -> $"Telegram '%A{msg}'" |> NotSupported |> Error |> async.Return
        | CallbackQuery dto ->
            Persistence.Dependencies.create cfg
            |> Result.bind (Consumer.Dependencies.create dto ct)
            |> ResultAsync.wrap (client |> Consume.callback dto.Value)
            |> ResultAsync.mapError (fun error -> error.add $"{dto.ChatId}")
        | _ -> $"Telegram '%A{data}'" |> NotSupported |> Error |> async.Return

let start client =
    fun cfg ct ->
        let handler = fun data -> consume data client cfg ct
        let client = Web.Client.Consumer.Telegram(client, handler)
        Web.Client.consume client ct
