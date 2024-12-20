module EA.Telegram.Routes.Embassies

open Infrastructure.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | Id of Graph.NodeId
    | All
    | Services of embassyId: Graph.NodeId * services: Services.GetRequest
    
    member this.Code =
        match this with
        | Id id -> "00" + Delimiter + id.Value
        | All -> "01"
        | Services (id, r) -> "02" + Delimiter + id.Value + Delimiter + r.Code
        
    static member parse(parts: string[]) =
        match parts.Length with
        | 0 -> All |> Ok
        | 1 -> parts[1] |> Graph.NodeIdValue |> GetRequest.Id |> Ok
        | _ -> parts[1..] |> Services.GetRequest.parse |> Result.map (fun r -> GetRequest.Services(parts[1] |> Graph.NodeIdValue, r))

type Request =
    | Get of GetRequest

    member this.Route =
        match this with
        | Get r -> r.Code
    
    static member parse (input: string) =
        let parts = input.Split Delimiter
        let remaining = parts[1..]
        
        match parts[0] with
        | "00" -> remaining |> GetRequest.parse |> Result.map Get
        | "01" -> remaining |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' route of Embassies" |> NotSupported |> Error