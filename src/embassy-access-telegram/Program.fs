open Infrastructure
open Infrastructure.Logging

[<EntryPoint>]
let main _ =

    System.Threading.CancellationToken.None
    |> EmbassyAccess.Telegram.Consumer.start
    |> ResultAsync.mapError (fun error -> error.Message |> Log.critical)
    |> Async.Ignore
    |> Async.Start

    0
