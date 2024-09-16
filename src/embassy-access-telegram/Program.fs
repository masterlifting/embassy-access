open System.Threading
open Infrastructure
open EmbassyAccess.Telegarm

[<EntryPoint>]
let main _ =
    let ct = CancellationToken.None

    let configuration = Configuration.getJson "appsettings"
    Logging.useConsole configuration

    "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
    |> Web.Telegram.Domain.EnvKey
    |> Web.Telegram.Client.create
    |> Result.map (fun client -> Web.Domain.Listener.Telegram(client, Receiver.receive ct client))
    |> Web.Client.listen ct
    |> ResultAsync.mapError (fun error -> error.Message |> Logging.Log.critical)
    |> Async.Ignore
    |> Async.RunSynchronously

    // wait for the log to be written
    Async.Sleep 500 |> Async.RunSynchronously
    0
