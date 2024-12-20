module EA.Telegram.Routes.Services

open Infrastructure.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | Id of Graph.NodeId
    | All

    member this.Code =
        match this with
        | Id id -> "00" + Delimiter + id.Value
        | All -> "01"

    static member parse(parts: string[]) =
        match parts.Length with
        | 0 -> All |> Ok
        | 1 -> parts[1] |> Graph.NodeIdValue |> GetRequest.Id |> Ok
        | _ -> $"'{parts}' for Services.GetRequest" |> NotSupported |> Error

type PostRequest =
    { ServiceId: Graph.NodeId
      EmbassyId: Graph.NodeId
      Payload: string }

    member this.Code =
        [ "10" + this.ServiceId.Value + this.EmbassyId.Value + this.Payload ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts.Length with
        | 3 ->
            { ServiceId = parts[0] |> Graph.NodeIdValue
              EmbassyId = parts[1] |> Graph.NodeIdValue
              Payload = parts[2] }
            |> Ok
        | _ -> $"'{parts}' for Services.PostRequest" |> NotSupported |> Error

type DeleteRequest =
    | Id of Graph.NodeId

    member this.Code =
        match this with
        | Id id -> "20" + Delimiter + id.Value

    static member parse(parts: string[]) =
        match parts.Length with
        | 1 -> parts[1] |> Graph.NodeIdValue |> DeleteRequest.Id |> Ok
        | _ -> $"'{parts}' for Services.DeleteRequest" |> NotSupported |> Error

type Request =
    | Get of GetRequest
    | Post of PostRequest
    | Delete of DeleteRequest

    member this.Route =
        match this with
        | Get r -> r.Code
        | Post r -> r.Code
        | Delete r -> r.Code

    static member parse (input: string) =
        let parts = input.Split Delimiter
        let remaining = parts[1..]

        match parts[0] with
        | "00" -> remaining |> GetRequest.parse |> Result.map Get
        | "01" -> remaining |> GetRequest.parse |> Result.map Get
        | "10" -> remaining |> PostRequest.parse |> Result.map Post
        | "20" -> remaining |> DeleteRequest.parse |> Result.map Delete
        | _ -> $"'{input}' route of Services" |> NotSupported |> Error
