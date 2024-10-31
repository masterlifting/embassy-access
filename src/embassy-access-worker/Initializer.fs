module internal EA.Worker.Initializer

open Infrastructure
open Worker.Domain

let initialize (configuration, ct) =
    configuration
    |> EA.Telegram.Consumer.start ct
    |> ResultAsync.map (fun _ -> Settings.AppName + " has been initialized." |> Info)
