[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure.Domain
open EA.Embassies.Russian.Domain
open EA.Embassies.Russian.Kdmid

module Service =
    open EA.Embassies.Russian.Kdmid.Domain

    let get name service =
        match service with
        | Kdmid service ->
            name
            |> service.Request.CreateRequest
            |> fun request ->
                { Request = request
                  TimeZone = service.Request.TimeZone }
            |> fun order -> service.Dependencies |> Order.start order
        | Midpass _ -> name |> NotSupported |> Error |> async.Return

module Order =
    module Kdmid =
        let start order deps = deps |> Order.start order
        let pick order deps = deps |> Order.pick order
