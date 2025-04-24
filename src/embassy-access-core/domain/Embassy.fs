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
    Name: string
    Description: string option
    TimeZone: float
} with

    interface Graph.INode with
        member this.Id = this.Id.Value
        member this.set id = { this with Id = id |> EmbassyId }
