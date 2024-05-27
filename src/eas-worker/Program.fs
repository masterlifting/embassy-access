﻿open Infrastructure
open Eas.Worker
open Eas

[<EntryPoint>]
let main _ =
    Logging.useConsole Configuration.AppSettings

    Core.Configuration.configure () |> Worker.Core.start |> Async.RunSynchronously

    0
