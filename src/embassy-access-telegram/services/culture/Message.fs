[<RequireQualifiedAccess>]
module EA.Telegram.Services.Culture.Message

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies
open EA.Telegram.Services.Culture.Payloads

let translateError culture error =
    fun (deps: Culture.Dependencies) ->
        deps
        |> Error.translate culture error
        |> Async.map (function
            | Ok error -> error
            | Error error -> error)

let translate culture message =
    fun (deps: Culture.Dependencies) ->
        match message with
        | Text payload -> deps |> Text.translate culture payload
        | ButtonsGroup payload -> deps |> ButtonsGroup.translate culture payload

let translateSeq culture messages =
    fun (deps: Culture.Dependencies) ->
        messages
        |> List.map (fun message -> deps |> translate culture message)
        |> Async.Sequential
        |> Async.map Result.choose

let translateRes culture msgRes =
    fun (deps: Culture.Dependencies) ->
        msgRes
        |> ResultAsync.bindAsync (fun message -> deps |> translate culture message)
        |> ResultAsync.mapErrorAsync (fun error -> deps |> translateError culture error)

let translateSeqRes culture msgSeqRes =
    fun (deps: Culture.Dependencies) ->
        msgSeqRes
        |> ResultAsync.bindAsync (fun messages -> deps |> translateSeq culture messages)
        |> ResultAsync.mapErrorAsync (fun error -> deps |> translateError culture error)
