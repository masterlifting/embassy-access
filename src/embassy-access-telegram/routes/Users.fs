module EA.Telegram.Routes.Users

open Infrastructure.Domain
open Web.Telegram.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | Id of ChatId
    | All
    | Embassies of userId: ChatId * embassies: Embassies.GetRequest

    member this.Code =
        match this with
        | Id id -> "00" + Delimiter + id.ValueStr
        | All -> "01"
        | Embassies(id, r) -> "02" + Delimiter + id.ValueStr + Delimiter + r.Code

    static member parse(parts: string[]) =
        match parts.Length with
        | 0 -> All |> Ok
        | 1 -> parts[1] |> ChatId.parse |> Result.map GetRequest.Id
        | _ ->
            parts[1..]
            |> Embassies.GetRequest.parse
            |> Result.bind (fun r ->
                parts[1]
                |> ChatId.parse
                |> Result.map (fun userId -> GetRequest.Embassies(userId, r)))

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
        | "02" -> remaining |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' route of Users" |> NotSupported |> Error