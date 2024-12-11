[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure.Domain
open EA.Embassies.Russian.Domain

module Service =
    let get service name =
        match service with
        | Kdmid service ->
            name
            |> service.Request.CreateRequest
            |> Kdmid.Domain.StartOrder.create service.Request.TimeZone
            |> Kdmid.Order.start service.Dependencies
        | Midpass _ -> name |> NotSupported |> Error |> async.Return

module Order =
    module Kdmid =
        let start deps order = order |> Kdmid.Order.start deps
        let pick deps order = order |> Kdmid.Order.pick deps
