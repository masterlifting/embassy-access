[<RequireQualifiedAccess>]
module EmbassyAccess.Deps

module Russian =

    let processRequest (storage, config, ct) =
        Embassies.Russian.Core.processRequestDeps ct config storage
        |> Api.ProcessRequestDeps.Russian
