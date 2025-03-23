[<RequireQualifiedAccess>]
module EA.Telegram.Services.Producer.Culture.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Producer
open EA.Telegram.Services.Culture
open EA.Telegram.Services.Culture.Payloads

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
        |> ResultAsync.mapErrorAsync (fun error -> deps.toBase () |> Command.translateError culture error)

let translateSeqRes culture msgSeqRes =
    fun (deps: Culture.Dependencies) ->
        msgSeqRes
        |> ResultAsync.bindAsync (fun messages -> deps |> translateSeq culture messages)
        |> ResultAsync.mapErrorAsync (fun error -> deps.toBase () |> Command.translateError culture error)
