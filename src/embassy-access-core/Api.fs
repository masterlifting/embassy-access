[<RequireQualifiedAccess>]
module EA.Api

open Infrastructure
open EA.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps

let getEmbassies () =
    [ Russian.Core.getCountries () |> Set.map Domain.Russian
      Russian.Core.getCountries () |> Set.map Domain.British
      Russian.Core.getCountries () |> Set.map Domain.French
      Russian.Core.getCountries () |> Set.map Domain.German
      Russian.Core.getCountries () |> Set.map Domain.Italian
      Russian.Core.getCountries () |> Set.map Domain.Spanish ]
    |> Seq.concat

let validateRequest (request: Domain.Request) =
    match request.Embassy with
    | Domain.Russian _ -> Russian.Core.validateRequest request
    | _ -> Error <| NotSupported "EmbassyAccess.Api.validateRequest"

let processRequest deps request =
    match deps with
    | Russian deps -> request |> Russian.Core.processRequest deps
