[<AutoOpen>]
module EA.Core.Domain.Service

open Infrastructure.Domain
open Infrastructure.Prelude

type ServiceId =
    | ServiceId of Tree.NodeId

    member this.NodeId =
        match this with
        | ServiceId id -> id

    member this.Value =
        match this with
        | ServiceId id -> id.Value

    static member create value =
        match value with
        | AP.IsString id -> Tree.NodeId.create id |> ServiceId |> Ok
        | _ -> $"ServiceId '{value}' is not supported." |> NotSupported |> Error

    static member split(ServiceId id) = id |> Tree.NodeId.split

    static member contains (parts: string seq) (ServiceId id) = id |> Tree.NodeId.contains parts

    static member combine(parts: string seq) =
        parts |> Tree.NodeId.combine |> ServiceId

    override this.ToString() = this.Value

type Service = {
    NameParts: string list
    Description: string option
} with

    member this.BuildName startWith delimiter =
        match this.NameParts.Length > startWith with
        | true -> this.NameParts |> List.skip startWith
        | false -> this.NameParts
        |> String.concat delimiter

    member this.LastName = this.NameParts |> List.tryLast |> Option.defaultValue "Unknown"
