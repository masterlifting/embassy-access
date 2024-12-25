module internal EA.Worker.Initializer

open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain

let run (_, cfg, ct) =
    async {
        cfg
        |> EA.Telegram.Consumer.Core.start ct
        |> ResultAsync.mapError (_.Message >> Log.critical)
        |> Async.Ignore
        |> Async.Start

        return "Has been initialized." |> Info |> Ok
    }
