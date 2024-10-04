module internal EmbassyAccess.Worker.Initializer

open Infrastructure
open Worker.Domain

let initialize () =
    fun (_, ct) ->
        Notifications.Telegram.listen ct
        |> ResultAsync.mapError (fun error -> error.Message |> Logging.Log.critical)
        |> Async.Ignore
        |> Async.Start

        Temporary.createTestData ct
        |> ResultAsync.map (fun data -> $"Test data has been created. Count: {data.Length}. ")
        |> ResultAsync.map (fun msg -> Success(msg + " Scheduler is running."))
    |> Some
