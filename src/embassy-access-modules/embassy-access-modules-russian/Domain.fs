module EA.Embassies.Russian.Domain

open EA.Embassies.Russian
open Infrastructure

type ServiceInfo =
    { Id: Graph.NodeId
      Name: string
      Instruction: string option }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.set(id, name) = { this with Id = id; Name = name }

module Midpass =

    type Service = { Request: Midpass.Domain.Request }

module Kdmid =

    type Service =
        { Request: Kdmid.Domain.ServiceRequest
          Dependencies: Kdmid.Domain.Dependencies }

type Service =
    | Midpass of Midpass.Service
    | Kdmid of Kdmid.Service

module External =
    open System

    type ServiceInfo() =
        member val Id: string = String.Empty with get, set
        member val Name: string = String.Empty with get, set
        member val Instruction: string option = None with get, set
        member val Children: ServiceInfo[] = [||] with get, set
