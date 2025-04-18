﻿[<AutoOpen>]
module EA.Core.Domain.ServiceNode

open Infrastructure.Domain

type ServiceNode = {
    Id: Graph.NodeId
    Name: string
    Instruction: string option
    Description: string option
} with

    interface Graph.INode with
        member this.Id = this.Id
        member this.set id = { this with Id = id }
