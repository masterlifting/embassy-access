[<AutoOpen>]
module EA.Core.Domain.EmbassyGraph

open Infrastructure.Domain
open Infrastructure.Prelude

type EmbassyGraph =
    { Id: Graph.NodeId
      Name: string
      Description: string option }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.set(id, name) = { this with Id = id; Name = name }
