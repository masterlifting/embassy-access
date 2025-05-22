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

    member this.FullName =
        match this.NameParts.Length > 2 with
        | true -> this.NameParts |> List.skip 2
        | false -> this.NameParts
        |> String.concat "."

    member this.ShortName = this.NameParts |> List.tryLast |> Option.defaultValue "Unknown"

    interface Graph.INode with
        member this.Id = this.Id.Value
        member this.set id = { this with Id = id |> ServiceId }

    static member print(service: Service) =
        let value = $"[Service] %s{service.FullName}"

        match service.Description with
        | Some description -> value + $"\n %s{description}"
        | None -> value
