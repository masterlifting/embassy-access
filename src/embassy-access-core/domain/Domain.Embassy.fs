[<AutoOpen>]
module EA.Core.Domain.Embassy

open Infrastructure

type Embassy =
    { Id: Graph.NodeId
      Name: string
      Description: string option }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.set(id, name) = { this with Id = id; Name = name }