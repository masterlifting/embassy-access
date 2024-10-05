open Infrastructure

[<EntryPoint>]
let main _ =

    let configuration = Configuration.getYaml "appsettings"
    Logging.useConsole configuration

    System.Threading.CancellationToken.None
    |> EmbassyAccess.Telegram.Consumer.start
    |> ResultAsync.mapError (fun error -> error.Message |> Logging.Log.critical)
    |> Async.Ignore
    |> Async.RunSynchronously

    0
