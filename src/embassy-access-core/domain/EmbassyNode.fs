[<AutoOpen>]
module EA.Core.Domain.EmbassyNode

open Infrastructure.Domain
open Infrastructure.Prelude

type EmbassyNode = {
    Id: Graph.NodeId
    Name: string
    ShortName: string
    Description: string option
    TimeZone: float option
} with

    interface Graph.INode with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.set(id, name) = { this with Id = id; Name = name }
