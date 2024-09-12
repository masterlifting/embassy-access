[<RequireQualifiedAccess>]
module EmbassyAccess.Deps

module Russian =

    let processRequest (storage, config, ct) =
        Embassies.Russian.Deps.processRequest ct config storage
        |> Api.ProcessRequestDeps.Russian

    let sendMessage ct message =
        Embassies.Russian.Deps.sendMessage ct message |> Api.SendMessageDeps.Russian

    let createListener ct context =
        Embassies.Russian.Deps.createListener ct context
        |> Api.ReceiveMessagesDeps.Russian
