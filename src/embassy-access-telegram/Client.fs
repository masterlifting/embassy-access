[<RequireQualifiedAccess>]
module EA.Telegram.Client

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram.Consumer
open EA.Telegram.Router
open EA.Telegram.Dependencies
open EA.Telegram.Controllers
open System.Diagnostics
open Infrastructure.Logging

let private respond payload =
    fun deps ->
        deps
        |> Request.Dependencies.create payload
        |> ResultAsync.wrap (fun deps ->
            Router.parse payload.Value
            |> ResultAsync.wrap (fun request -> deps |> Controller.respond request))

let private processData data =
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
        |> Async.apply (
            let taskName = "Telegram response"
            let currentProcessMemory = Process.GetCurrentProcess().WorkingSet64 / 1024L
            let gcTotalMemory = GC.GetTotalMemory false / 1024L
            Log.wrn $"{taskName}Memory usage: %u{currentProcessMemory} KB"
            Log.wrn $"{taskName}GC total memory: %u{gcTotalMemory} KB"
            Ok() |> async.Return
        )

let start () =
    fun (deps: Client.Dependencies) ->
        let processData data = deps |> processData data
        deps.Web.Telegram.start processData
