module EA.Embassies.Russian.Domain

open EA.Embassies.Russian
open Infrastructure

type ServiceInfo =
    { Id: Graph.NodeId
      Name: string
      Instruction: string option
      Description: string option }

    interface Graph.INodeName with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.set(id, name) = { this with Id = id; Name = name }

type Service =
    | Midpass of Midpass.Domain.Service
    | Kdmid of Kdmid.Domain.Service

module External =
    open System

    type ServiceInfo() =
        member val Id: string = String.Empty with get, set
        member val Name: string = String.Empty with get, set
        member val Instruction: string option = None with get, set
        member val Description: string option = None with get, set
        member val Children: ServiceInfo[] = [||] with get, set
