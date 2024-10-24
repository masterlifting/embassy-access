[<RequireQualifiedAccess>]
module EA.Api

open Infrastructure
open EA.Embassies

type ProcessRequestDeps = Russian of Russian.Domain.ProcessRequestDeps

let getEmbassies () =
    [ Russian.Core.getCountries () |> Set.map Domain.Russian ] |> Seq.concat |> Set.ofSeq
    
let getEmbassyCountries embassy =
    match embassy with
    | Domain.Russian _ -> Russian.Core.getCountries ()
    | _ -> Set.empty
    
let getEmbassyCountryCities embassy country =
    getEmbassyCountries embassy
    |> Set.filter (fun x -> x = country)
    |> Set.map (_.City)

let validateRequest (request: Domain.Request) =
    match request.Embassy with
    | Domain.Russian _ -> Russian.Core.validateRequest request
    | _ -> Error <| NotSupported "EmbassyAccess.Api.validateRequest"

let processRequest deps request =
    match deps with
    | Russian deps -> request |> Russian.Core.processRequest deps
