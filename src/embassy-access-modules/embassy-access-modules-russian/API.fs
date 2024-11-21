[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure
open EA.Embassies.Russian.Domain

let SUPPORTED_CITIES = Kdmid.Domain.Constants.SUPPORTED_SUB_DOMAINS.Values

module Service =
    let get service =
        match service with
        | Passport service ->
            match service with
            | IssueForeign(deps, request) ->
                service.Name
                |> request.CreateRequest
                |> Kdmid.Domain.StartOrder.create request.TimeZone
                |> Kdmid.Order.start deps
            | CheckReadiness _ -> service.Name |> NotSupported |> Error |> async.Return
        | Notary service ->
            match service with
            | PowerOfAttorney(deps, request) ->
                service.Info
                |> request.CreateRequest
                |> Kdmid.Domain.StartOrder.create request.TimeZone
                |> Kdmid.Order.start deps
        | Citizenship service ->
            match service with
            | CitizenshipRenunciation(deps, request) ->
                service.Info
                |> request.CreateRequest
                |> Kdmid.Domain.StartOrder.create request.TimeZone
                |> Kdmid.Order.start deps
                
module Order =
    module Kdmid =
        let start deps order = order |> Kdmid.Order.start deps
        let pick deps order = order |> Kdmid.Order.pick deps
