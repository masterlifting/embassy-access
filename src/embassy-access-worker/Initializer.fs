module internal EA.Worker.Initializer

open Infrastructure
open Infrastructure.Logging
open Worker.Domain

let run (_, configuration, ct) =
    async {
        configuration
        |> EA.Telegram.Consumer.start ct
        |> ResultAsync.mapError (_.Message >> Log.critical)
        |> Async.Ignore
        |> Async.Start

        return "Has been initialized." |> Info |> Ok
    }
