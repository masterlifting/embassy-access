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
    Name: string list
    Description: string option
    TimeZone: float
} with

    member this.FullName =
        match this.Name.Length > 1 with
        | true -> this.Name |> List.skip 1 |> String.concat "."
        | false -> this.Name |> String.concat ""

    member this.ShortName = this.Name |> List.tryLast |> Option.defaultValue "Unknown"

    interface Graph.INode with
        member this.Id = this.Id.Value
        member this.set id = { this with Id = id |> EmbassyId }

    static member print(embassy: Embassy) =
        let id = embassy.Id.ValueStr
        let name = embassy.FullName

        let value = "[Embassy]" + $"\n '%s{id}'" + $"\n %s{name}"

        match embassy.Description with
        | Some description -> value + $"\n %s{description}"
        | None -> value
