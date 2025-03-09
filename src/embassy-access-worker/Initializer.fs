module internal EA.Worker.Initializer

open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Worker.Dependencies

let run (_, cfg, ct) =
    async {
        Telegram.Consumer.create cfg ct
        |> ResultAsync.wrap EA.Telegram.Consumer.start
        |> ResultAsync.mapError (_.Message >> Log.critical)
        |> Async.Ignore
        |> Async.Start

        Persistence.Dependencies.create cfg
        |> ResultAsync.wrap _.resetData()
        |> ResultAsync.mapError (_.Message >> Log.critical)
        |> Async.Ignore
        |> Async.Start

        return "Services have been initialized. Data has been reset." |> Info |> Ok
    }
