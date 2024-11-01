module internal EA.Worker.Initializer

open Infrastructure
open Infrastructure.Logging
open Worker.Domain

let initialize (configuration, ct) =
    async {
        configuration
        |> EA.Telegram.Consumer.start ct
        |> ResultAsync.mapError (_.Message >> Log.critical)
        |> Async.Ignore
        |> Async.Start

        return Settings.AppName + " has been initialized." |> Info |> Ok
    }
