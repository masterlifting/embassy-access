module EA.Telegram.Consumer

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Consumer
open EA.Telegram.Endpoints.Request
open EA.Telegram.Dependencies
open EA.Telegram.Controllers.Consumer

let private respond payload =
    fun deps ->
        deps
        |> Request.Dependencies.create payload
        |> ResultAsync.wrap (fun deps ->
            Request.parse payload.Value
            |> ResultAsync.wrap (fun request -> deps |> Controller.respond request))
        |> ResultAsync.mapError (fun error -> error.extendMsg $"{payload.ChatId}")

let consume data =
    fun deps ->
        match data with
        | Message msg ->
            match msg with
            | Text payload -> deps |> respond payload
            | _ -> $"Telegram '%A{msg}'" |> NotSupported |> Error |> async.Return
        | CallbackQuery payload -> deps |> respond payload
        | _ -> $"Telegram '%A{data}'" |> NotSupported |> Error |> async.Return

let start (deps: Consumer.Dependencies) =
    let handler = fun data -> consume data deps
    let consumer = Web.Client.Consumer.Telegram(deps.Web.Telegram.Client, handler)
    Web.Client.consume consumer deps.CancellationToken
