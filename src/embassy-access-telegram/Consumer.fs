module EA.Telegram.Consumer

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Consumer
open EA.Telegram.Endpoints.Request
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Controllers.Consumer

let private respond value =
    fun deps ->
        Request.parse value
        |> ResultAsync.wrap (fun request -> deps |> Controller.respond request)

let consume data =
    fun client cfg ct ->
        match data with
        | Message msg ->
            match msg with
            | Text payload ->
                Persistence.Dependencies.create cfg
                |> Result.bind (fun pDeps ->
                    AIProvider.Dependencies.create cfg |> Result.map (fun aiDeps -> pDeps, aiDeps))
                |> Result.bind (Consumer.Dependencies.create client payload ct)
                |> ResultAsync.wrap (respond payload.Value)
                |> ResultAsync.mapError (fun error -> error.extendMsg $"{payload.ChatId}")
            | _ -> $"Telegram '%A{msg}'" |> NotSupported |> Error |> async.Return
        | CallbackQuery payload ->
            Persistence.Dependencies.create cfg
            |> Result.bind (fun pDeps -> AIProvider.Dependencies.create cfg |> Result.map (fun aiDeps -> pDeps, aiDeps))
            |> Result.bind (Consumer.Dependencies.create client payload ct)
            |> ResultAsync.wrap (respond payload.Value)
            |> ResultAsync.mapError (fun error -> error.extendMsg $"{payload.ChatId}")
        | _ -> $"Telegram '%A{data}'" |> NotSupported |> Error |> async.Return

let start client =
    fun cfg ct ->
        let handler = fun data -> consume data client cfg ct
        let consumer = Web.Client.Consumer.Telegram(client, handler)
        Web.Client.consume consumer ct
