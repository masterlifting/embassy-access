module EA.Telegram.Routes.Services

open Infrastructure.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | Service of Graph.NodeId

    member this.Code =
        match this with
        | Service id -> [ "00"; id.Value ] |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; id |] -> id |> Graph.NodeIdValue |> GetRequest.Service |> Ok
        | _ -> $"'{parts}' for Services.GetRequest" |> NotSupported |> Error

type Request =
    | Get of GetRequest

    member this.Route =
        match this with
        | Get r -> r.Code

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '0' -> parts |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' route of Services" |> NotSupported |> Error
