open System.Threading
open Infrastructure

[<EntryPoint>]
let main _ =
    let ct = CancellationToken.None

    let configuration = Configuration.getJson "appsettings"
    Logging.useConsole configuration

    "EMBASSY_ACCESS_TELEGRAM_BOT_TOKEN"
    |> Web.Telegram.Domain.EnvKey
    |> Web.Domain.Telegram
    |> EmbassyAccess.Api.receiveMessages ct
    |> ResultAsync.mapError (fun error -> error.Message |> Logging.Log.critical)
    |> Async.Ignore
    |> Async.RunSynchronously

    0
