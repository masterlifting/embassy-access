[<RequireQualifiedAccess>]
module EmbassyAccess.Api

type ProcessRequestDeps = Russian of Embassies.Russian.Domain.ProcessRequestDeps

let processRequest deps request =
    match deps with
    | Russian deps -> request |> Embassies.Russian.Core.processRequest deps
