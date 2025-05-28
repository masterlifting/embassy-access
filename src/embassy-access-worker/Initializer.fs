module internal EA.Worker.Initializer

open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Worker.Dependencies

let run (task, cfg, ct) =
    async {

        Telegram.Dependencies.create cfg ct
        |> ResultAsync.wrap (fun deps -> deps.Culture.setContext () |> ResultAsync.map (fun _ -> deps))
        |> ResultAsync.bindAsync (EA.Telegram.Client.start ())
        |> ResultAsync.mapError (_.Message >> Log.crt)
        |> Async.Ignore
        |> Async.Start

        return
            $"%s{ActiveTask.print task} Required services have been initialized."
            |> Log.scs
            |> Ok
    }
