[<AutoOpen>]
module EA.Core.Domain.Embassy

open Infrastructure.Domain
open Infrastructure.Prelude

type EmbassyId =
    | EmbassyId of Tree.NodeId

    member this.Value =
        match this with
        | EmbassyId id -> id

    member this.ValueStr = this.Value.Value

type Embassy = {
    Id: EmbassyId
    NameParts: string list
    Description: string option
    TimeZone: float
} with

    member this.BuildName startWith delimiter =
        match this.NameParts.Length > startWith with
        | true -> this.NameParts |> List.skip startWith
        | false -> this.NameParts
        |> String.concat delimiter

    member this.LastName = this.NameParts |> List.tryLast |> Option.defaultValue "Unknown"

    interface Tree.INode with
        member this.Id = this.Id.Value
        member this.set id = { this with Id = id |> EmbassyId }
