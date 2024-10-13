open System.Threading
open Infrastructure

[<EntryPoint>]
let main _ =

    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    configuration
    |> EA.Telegram.Consumer.start CancellationToken.None
    |> ResultAsync.mapError (fun error -> error.Message |> Logging.Log.critical)
    |> Async.Ignore
    |> Async.RunSynchronously

    0
