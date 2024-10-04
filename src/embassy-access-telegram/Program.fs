[<EntryPoint>]
let main _ =

    Notifications.Telegram.listen ct
    |> ResultAsync.mapError (fun error -> error.Message |> Logging.Log.critical)
    |> Async.Ignore
    |> Async.Start

    0
