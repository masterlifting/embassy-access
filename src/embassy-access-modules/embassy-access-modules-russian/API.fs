[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure
open EA.Embassies.Russian.Domain

let processService service =
    match service with
    | Passport service ->
        match service with
        | IssueForeign request -> service.Name |> request.Create |> Kdmid.API.processRequest
        | CheckReadiness _ -> fun _ -> service.Name |> NotSupported |> Error |> async.Return
    | Notary service ->
        match service with
        | PowerOfAttorney request -> service.Name |> request.Create |> Kdmid.API.processRequest
    | Citizenship service ->
        match service with
        | CitizenshipRenunciation request -> service.Name |> request.Create |> Kdmid.API.processRequest
