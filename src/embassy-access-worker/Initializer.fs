module internal EA.Worker.Initializer

open Infrastructure.Prelude
open Infrastructure.Logging
open Worker.Domain
open EA.Worker.Dependencies

let run (task, cfg, ct) =
    async {

        Telegram.Dependencies.create cfg ct
        |> ResultAsync.wrap (fun deps ->
            deps.Culture.setContext ()
            |> ResultAsync.bindAsync (fun _ ->
                let processData data =
                    deps |> EA.Telegram.Client.processData data
                deps.Web.Telegram.start processData))
        |> ResultAsync.mapError (_.Message >> Log.crt)
        |> Async.Ignore
        |> Async.Start

        return
            $"%s{ActiveTask.print task} Required services have been initialized."
            |> Log.scs
            |> Ok
    }
