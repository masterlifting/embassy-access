[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure.Domain
open EA.Embassies.Russian.Domain
open EA.Embassies.Russian.Kdmid

module Service =

    let get service =
        match service with
        | Kdmid service -> service.Dependencies |> Order.start service.Order
        | Midpass _ -> "Midpass service" |> NotSupported |> Error |> async.Return

module Order =
    module Kdmid =
        let start order deps = deps |> Order.start order
        let pick order deps = deps |> Order.pick order
