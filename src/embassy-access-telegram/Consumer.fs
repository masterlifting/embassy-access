module EA.Telegram.Consumer

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Consumer
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Consumer

module private Consume =
    open EA.Telegram.Controllers.Consumer
    open EA.Telegram.Endpoints.Consumer.Request

    let text value =
        fun deps ->
            value
            |> Request.parse
            |> ResultAsync.wrap (fun request -> deps |> Consumer.respond request)

    let callback value =
        fun deps ->
            value
            |> Request.parse
            |> ResultAsync.wrap (fun request -> deps |> Consumer.respond request)

let consume data =
    fun client cfg ct ->
        match data with
        | Message msg ->
            match msg with
            | Text dto ->
                Persistence.Dependencies.create cfg
                |> Result.bind (Consumer.Dependencies.create client dto ct)
                |> ResultAsync.wrap (Consume.text dto.Value)
                |> ResultAsync.mapError (fun error -> error.extend $"{dto.ChatId}")
            | _ -> $"Telegram '%A{msg}'" |> NotSupported |> Error |> async.Return
        | CallbackQuery dto ->
            Persistence.Dependencies.create cfg
            |> Result.bind (Consumer.Dependencies.create client dto ct)
            |> ResultAsync.wrap (Consume.callback dto.Value)
            |> ResultAsync.mapError (fun error -> error.extend $"{dto.ChatId}")
        | _ -> $"Telegram '%A{data}'" |> NotSupported |> Error |> async.Return

let start client =
    fun cfg ct ->
        let handler = fun data -> consume data client cfg ct
        let consumer = Web.Client.Consumer.Telegram(client, handler)
        Web.Client.consume consumer ct
