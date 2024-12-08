[<AutoOpen>]
module EA.Embassies.Russian.Domain.Service

open EA.Embassies.Russian

type Service =
    | Midpass of Midpass.Domain.Service
    | Kdmid of Kdmid.Domain.Service
