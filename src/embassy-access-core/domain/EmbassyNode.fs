[<AutoOpen>]
module EA.Core.Domain.EmbassyNode

open Infrastructure.Domain
open Infrastructure.Prelude

type EmbassyNode = {
    Id: Graph.NodeId
    Name: string
    Description: string option
    TimeZone: float
} with

    interface Graph.INode with
        member this.Id = this.Id
        member this.set id = { this with Id = id }
