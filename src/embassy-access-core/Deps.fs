[<RequireQualifiedAccess>]
module EmbassyAccess.Deps

module Russian =

    let processRequest (storage, config, ct) =
        Embassies.Russian.Deps.processRequest ct config storage
        |> Api.ProcessRequestDeps.Russian

    let sendNotification (ct) =
        Embassies.Russian.Deps.sendNotification ct |> Api.SendNotificationDeps.Russian
