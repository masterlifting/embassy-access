﻿module internal EA.Worker.Initializer

open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Worker.Dependencies

let run (_, cfg, ct) =
    let inline startTelegramBotConsumer client =
        EA.Telegram.Consumer.start client cfg ct

    async {
        Web.Dependencies.create ()
        |> Result.bind _.initTelegramClient()
        |> ResultAsync.wrap startTelegramBotConsumer
        |> ResultAsync.mapError (_.Message >> Log.critical)
        |> Async.Ignore
        |> Async.Start

        return "Has been initialized." |> Info |> Ok
    }
