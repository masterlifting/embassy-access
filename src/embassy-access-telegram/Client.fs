[<RequireQualifiedAccess>]
module EA.Telegram.Client

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Consumer
open EA.Telegram.Router
open EA.Telegram.Dependencies
open EA.Telegram.Controllers

let private respond payload =
    fun deps ->
        deps
        |> Request.Dependencies.create payload
        |> ResultAsync.wrap (fun deps ->
            Router.parse payload.Value
            |> ResultAsync.wrap (fun request -> deps |> Controller.respond request))
        |> ResultAsync.mapError (fun error -> error.extendMsg $"{payload.ChatId}")

let private consume data =
    fun deps ->
        match data with
        | Message msg ->
            match msg with
            | Text payload -> deps |> respond payload
            | _ -> $"Telegram '%A{msg}'" |> NotSupported |> Error |> async.Return
        | CallbackQuery payload -> deps |> respond payload
        | _ -> $"Telegram '%A{data}'" |> NotSupported |> Error |> async.Return

let listen (deps: Client.Dependencies) =
    let handler = fun data -> consume data deps
    let consumer = Web.Client.Consumer.Telegram(deps.Web.Telegram.Client, handler)
    Web.Client.consume consumer deps.CancellationToken
