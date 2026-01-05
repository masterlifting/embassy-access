[<AutoOpen>]
module EA.Core.Domain.Embassy

open Infrastructure.Domain
open Infrastructure.Prelude

type EmbassyId =
    | EmbassyId of Tree.NodeId

    member this.NodeId =
        match this with
        | EmbassyId id -> id

    member this.Value =
        match this with
        | EmbassyId id -> id.Value

    static member create value =
        match value with
        | AP.IsString id -> Tree.NodeId.create id |> EmbassyId |> Ok
        | _ -> $"EmbassyId '{value}' is not supported." |> NotSupported |> Error

    override this.ToString() = this.Value

type Embassy = {
    NameParts: string list
    Description: string option
} with

    member this.BuildName startWith delimiter =
        match this.NameParts.Length > startWith with
        | true -> this.NameParts |> List.skip startWith
        | false -> this.NameParts
        |> String.concat delimiter

    member this.LastName = this.NameParts |> List.tryLast |> Option.defaultValue "Unknown"
