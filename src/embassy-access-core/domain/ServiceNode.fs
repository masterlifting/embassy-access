[<AutoOpen>]
module EA.Core.Domain.ServiceNode

open Infrastructure.Domain

type ServiceNode =
    { Id: Graph.NodeId
      Name: string
      ShortName: string
      Instruction: string option
      Description: string option }

    interface Graph.INode with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.set(id, name) = { this with Id = id; Name = name }
