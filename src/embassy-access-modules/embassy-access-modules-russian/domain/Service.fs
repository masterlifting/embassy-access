[<AutoOpen>]
module EA.Embassies.Russian.Domain.Service

open EA.Embassies.Russian.Midpass.Domain
open EA.Embassies.Russian.Kdmid.Domain
open EA.Embassies.Russian.Kdmid.Dependencies

type KdmidService =
    { Request: KdmidRequest
      Dependencies: Order.Dependencies }

type Service =
    | Midpass of MidpassService
    | Kdmid of KdmidService
