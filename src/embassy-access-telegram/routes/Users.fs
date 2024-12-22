module EA.Telegram.Routes.Users

open Infrastructure.Domain
open Web.Telegram.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | Id of ChatId
    | All
    | Embassies of ChatId

    member this.Code =
        match this with
        | Id id -> "00" + Delimiter + id.ValueStr
        | All -> "01"
        | Embassies id -> "02" + Delimiter + id.ValueStr

    static member parse(parts: string[]) =
        match parts.Length with
        | 0 -> All |> Ok
        | 1 ->
            match parts[1] with
            | "00" -> parts[2] |> ChatId.parse |> Result.map Id
            | "02" -> parts[2] |> ChatId.parse |> Result.map Embassies
            | _ ->
                $"Invalid parts length {parts.Length} for Users.GetRequest"
                |> NotSupported
                |> Error
        | _ ->
            $"Invalid parts length {parts.Length} for Users.GetRequest"
            |> NotSupported
            |> Error

type Request =
    | Get of GetRequest

    member this.Route =
        match this with
        | Get r -> r.Code

    static member parse(input: string) =
        let parts = input.Split Delimiter
        let remaining = parts[1..]

        match parts[0] with
        | "00" -> remaining |> GetRequest.parse |> Result.map Get
        | "01" -> remaining |> GetRequest.parse |> Result.map Get
        | "02" -> remaining |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' route of Users" |> NotSupported |> Error
