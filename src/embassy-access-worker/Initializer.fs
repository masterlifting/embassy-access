module internal EA.Worker.Initializer

open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain

let run (_, cfg, ct) =
    async {
        Dependencies.Persistence.Dependencies.create cfg
        |> Result.bind (fun deps -> deps.initTelegramClient ())
        |> ResultAsync.wrap (fun client -> EA.Telegram.Consumer.start client cfg ct)
        |> ResultAsync.mapError (_.Message >> Log.critical)
        |> Async.Ignore
        |> Async.Start

        return "Has been initialized." |> Info |> Ok
    }
