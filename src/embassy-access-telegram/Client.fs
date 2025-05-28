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

let public processData data =
    fun deps ->
        match data with
        | Message msg ->
            match msg with
            | Text payload -> deps |> respond payload
            | _ ->
                $"Telegram message: '%A{msg}' is not supported."
                |> NotSupported
                |> Error
                |> async.Return
        | CallbackQuery payload -> deps |> respond payload
        | _ ->
            $"Telegram message: '%A{data}' is not supported."
            |> NotSupported
            |> Error
            |> async.Return
