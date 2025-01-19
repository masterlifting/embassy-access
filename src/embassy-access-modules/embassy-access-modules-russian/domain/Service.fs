[<AutoOpen>]
module EA.Embassies.Russian.Domain.Service

open EA.Core.Domain
open EA.Embassies.Russian

type KdmidService =
    { Request: Request
      Dependencies: Kdmid.Dependencies.Order.Dependencies }

type MidpassService =
    { Request: Midpass.Domain.MidpassRequest.MidpassRequest }

type Service =
    | Midpass of MidpassService
    | Kdmid of KdmidService
