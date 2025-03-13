﻿module EA.Telegram.Services.Culture.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Culture.Payloads

let setCulture (culture: Culture) =
    fun (deps: Culture.Dependencies) ->
        culture
        |> deps.setCurrentCulture
        |> ResultAsync.map (fun _ ->
            (deps.ChatId, Replace deps.MessageId)
            |> Text.create $"The language has been changed to {culture.Name}")

let setCultureCallback (culture: Culture) =
    fun (deps: Culture.Dependencies) -> culture |> deps.setCurrentCulture

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
        |> Async.bind (function
            | Ok message -> deps |> translate culture message
            | Error error -> deps |> Error.translate culture error |> Async.map Error)

let translateSeqRes culture msgSeqRes =
    fun (deps: Culture.Dependencies) ->
        msgSeqRes
        |> Async.bind (function
            | Ok messages -> deps |> translateSeq culture messages
            | Error error -> deps |> Error.translate culture error |> Async.map Error)
