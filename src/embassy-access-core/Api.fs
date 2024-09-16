[<RequireQualifiedAccess>]
module EmbassyAccess.Api

open EmbassyAccess.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps

let getEmbassies () =
    Set [ Russian.Core.getCountries () |> Set.map Domain.Russian ]

let processRequest deps request =
    match deps with
    | Russian deps -> request |> Russian.Core.processRequest deps
