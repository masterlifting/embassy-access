[<RequireQualifiedAccess>]
module EA.Telegram.Services.Producer.Culture.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Domain.Producer
open AIProvider.Services.Dependencies
open EA.Telegram.Services.Culture.Payloads

let translate culture message placeholder =
    fun (deps: Culture.Dependencies) ->
        match message with
        | Text payload -> deps |> Text.translate culture placeholder payload
        | ButtonsGroup payload -> deps |> ButtonsGroup.translate culture placeholder payload

let translateSeq culture messages placeholder =
    fun (deps: Culture.Dependencies) ->
        messages
        |> List.map (fun message -> deps |> translate culture message placeholder)
        |> Async.Sequential
        |> Async.map Result.choose

let translateError culture error placeholder =
    fun (deps: Culture.Dependencies) ->
        deps
        |> Error.translate culture placeholder error
        |> Async.map (function
            | Ok error -> error
            | Error error -> error)

let translateRes culture msgRes placeholder =
    fun (deps: Culture.Dependencies) ->
        msgRes
        |> ResultAsync.bindAsync (fun message -> deps |> translate culture message placeholder)
        |> ResultAsync.mapErrorAsync (fun error -> deps |> translateError culture error placeholder)

let translateSeqRes culture msgSeqRes placeholder =
    fun (deps: Culture.Dependencies) ->
        msgSeqRes
        |> ResultAsync.bindAsync (fun messages -> deps |> translateSeq culture messages placeholder)
        |> ResultAsync.mapErrorAsync (fun error -> deps |> translateError culture error placeholder)
