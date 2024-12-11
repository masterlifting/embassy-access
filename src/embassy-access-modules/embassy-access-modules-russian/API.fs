[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure.Domain
open EA.Embassies.Russian.Domain
open EA.Embassies.Russian.Kdmid

module Service =
    open EA.Embassies.Russian.Kdmid.Domain

    let get service name =
        match service with
        | Kdmid service ->
            name
            |> service.Request.CreateRequest
            |> StartOrder.create service.Request.TimeZone
            |> Order.start service.Dependencies
        | Midpass _ -> name |> NotSupported |> Error |> async.Return

module Order =
    module Kdmid =
        let start deps order = order |> Order.start deps
        let pick deps order = order |> Order.pick deps
