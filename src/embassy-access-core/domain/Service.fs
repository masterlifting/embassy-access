[<AutoOpen>]
module EA.Core.Domain.Service

open Infrastructure.Domain

type ServiceId =
    | ServiceId of Graph.NodeId

    member this.Value =
        match this with
        | ServiceId id -> id

    member this.ValueStr = this.Value.Value

type Service = {
    Id: ServiceId
    Name: string
    Description: string option
} with

    interface Graph.INode with
        member this.Id = this.Id.Value
        member this.set id = { this with Id = id |> ServiceId }
        
    static member print (service: Service) =
        let id = service.Id.ValueStr
        let name = service.Name
        let description = service.Description |> Option.defaultValue ""
        $"Service: {id}, Name: {name}, Description: {description}"
