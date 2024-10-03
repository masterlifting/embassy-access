[<RequireQualifiedAccess>]
module EmbassyAccess.Api

open Infrastructure
open EmbassyAccess.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps

let getEmbassies () =
    Set [ Russian.Core.getCountries () |> Set.map Domain.Russian ]

let validateRequest (request: Domain.Request) =
    match request.Embassy with
    | Domain.Russian _ -> Russian.Core.validateRequest request
    | _ -> Error <| NotSupported "EmbassyAccess.Api.validateRequest"

let processRequest deps request =
    match deps with
    | ProcessRequestDeps.Russian deps -> request |> Russian.Core.processRequest deps
