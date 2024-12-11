[<AutoOpen>]
module EA.Embassies.Russian.Domain.Service

open EA.Embassies.Russian.Midpass.Domain
open EA.Embassies.Russian.Kdmid.Domain

type Service =
    | Midpass of MidpassService
    | Kdmid of KdmidService
