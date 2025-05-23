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
    NameParts: string list
    Description: string option
} with

    member this.BuildName startWith delimiter =
        match this.NameParts.Length > startWith with
        | true -> this.NameParts |> List.skip startWith
        | false -> this.NameParts
        |> String.concat delimiter

    member this.LastName = this.NameParts |> List.tryLast |> Option.defaultValue "Unknown"

    interface Graph.INode with
        member this.Id = this.Id.Value
        member this.set id = { this with Id = id |> ServiceId }
