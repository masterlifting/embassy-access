open System.Threading
open Infrastructure
open EmbassyAccess.Telegarm

[<EntryPoint>]
let main _ =
    let ct = CancellationToken.None

    let configuration = Configuration.getJson "appsettings"
    Logging.useConsole configuration

    let createReceiver ct context =
        match context with
        | Web.Domain.Telegram token ->
            Web.Telegram.Client.create token
            |> Result.map (fun client -> Web.Domain.Listener.Telegram(client, Receiver.receive ct client))
        | _ -> Error <| NotSupported $"Context '{context}'."

    "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
    |> Web.Telegram.Domain.EnvKey
    |> Web.Domain.Telegram
    |> createReceiver ct
    |> Web.Client.listen ct
    |> ResultAsync.mapError (fun error -> error.Message |> Logging.Log.critical)
    |> Async.Ignore
    |> Async.RunSynchronously

    // wait for the log to be written
    Async.Sleep 500 |> Async.RunSynchronously
    0
