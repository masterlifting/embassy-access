﻿module internal EA.Worker.Initializer

open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Worker.Dependencies

let run (task, cfg, ct) =
    async {
        Telegram.Dependencies.create cfg ct
        |> ResultAsync.wrap EA.Telegram.Client.listen
        |> ResultAsync.mapError (_.Message >> Log.crt)
        |> Async.Ignore
        |> Async.Start

        Persistence.Dependencies.create cfg
        |> ResultAsync.wrap _.resetData()
        |> ResultAsync.mapError (_.Message >> Log.crt)
        |> Async.Ignore
        |> Async.Start

        return $"%s{ActiveTask.print task} Services have been initialized." |> Log.scs |> Ok
    }
