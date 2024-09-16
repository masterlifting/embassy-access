module internal EmbassyAccess.Worker.Initialize

open System
open Infrastructure
open Worker.Domain

let createTask () =
    Some
    <| fun (_, _, ct) ->
        EmbassyAccess.Worker.Notifications.Telegram.listen ct
        |> Async.Ignore
        |> Async.Start

        Temporary.createTestData ct
        |> ResultAsync.map (fun data -> $"Test data has been created. Count: {data.Length}")
        |> ResultAsync.map (fun msg -> Success(msg + Environment.NewLine + "Scheduler is running."))
