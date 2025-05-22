[<AutoOpen>]
module EA.Core.Domain.Embassy

open Infrastructure.Domain
open Infrastructure.Prelude

type EmbassyId =
    | EmbassyId of Graph.NodeId

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

    member this.FullName =
        match this.NameParts.Length > 1 with
        | true -> this.NameParts |> List.skip 1
        | false -> this.NameParts
        |> String.concat "."

    member this.ShortName = this.NameParts |> List.tryLast |> Option.defaultValue "Unknown"

    interface Graph.INode with
        member this.Id = this.Id.Value
        member this.set id = { this with Id = id |> EmbassyId }

    static member print(embassy: Embassy) =
        let value = $"[Embassy] %s{embassy.FullName}"

        match embassy.Description with
        | Some description -> value + $"\n %s{description}"
        | None -> value
