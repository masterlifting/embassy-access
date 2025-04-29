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

    static member print(embassy: Embassy) =
        let id = embassy.Id.ValueStr
        let name = embassy.Name
        let timeZone = embassy.TimeZone

        match embassy.Description with
        | Some description ->
            $"Embassy -> Id '%s{id}' Name: '%s{name}' Description: '%s{description}, TimeZone: '%f{timeZone}'"
        | None -> $"Embassy -> Id '%s{id}' Name: '%s{name}' TimeZone: '%f{timeZone}'"
