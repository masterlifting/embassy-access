[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure
open EA.Embassies.Russian.Domain

let getService service =
    match service with
    | Passport service ->
        match service with
        | IssueForeign (deps, request) -> service.Name |> request.Create |> Kdmid.API.processRequest deps
        | CheckReadiness _ -> fun _ -> service.Name |> NotSupported |> Error |> async.Return
    | Notary service ->
        match service with
        | PowerOfAttorney (deps, request) -> service.Name |> request.Create |> Kdmid.API.processRequest deps
    | Citizenship service ->
        match service with
        | CitizenshipRenunciation (deps, request) -> service.Name |> request.Create |> Kdmid.API.processRequest deps
