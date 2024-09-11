[<RequireQualifiedAccess>]
module EmbassyAccess.Deps

module Russian =

    let processRequest (storage, config, ct) =
        Embassies.Russian.Deps.processRequest ct config storage
        |> Api.ProcessRequestDeps.Russian

    let sendMessage ct =
        Embassies.Russian.Deps.sendMessage ct |> Api.SendMessageDeps.Russian

    let listener ct client =
        Embassies.Russian.Deps.listener ct client |> Api.ReceiveMessageDeps.Russian
