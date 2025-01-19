[<RequireQualifiedAccess>]
module EA.Embassies.Russian.API

open Infrastructure.Domain
open EA.Embassies.Russian
open EA.Embassies.Russian.Domain

module Service =

    let get service =
        match service with
        | Kdmid service -> service.Dependencies |> Kdmid.Order.start service.Request
        | Midpass _ -> "Midpass service" |> NotSupported |> Error |> async.Return

module Order =
    module Kdmid =
        let start = Kdmid.Order.start
        let pick = Kdmid.Order.pick
