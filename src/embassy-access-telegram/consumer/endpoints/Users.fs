module EA.Telegram.Consumer.Endpoints.Users

open Infrastructure.Domain
open Web.Telegram.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | UserEmbassies of ChatId
    | UserEmbassy of userId: ChatId * embassyId: Graph.NodeId

    member this.Code =
        match this with
        | UserEmbassies id -> [ "00"; id.ValueStr ]
        | UserEmbassy(userId, embassyId) -> [ "01"; userId.ValueStr; embassyId.Value ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; id |] -> id |> ChatId.parse |> Result.map GetRequest.UserEmbassies
        | [| "01"; userId; embassyId |] ->
            userId
            |> ChatId.parse
            |> Result.map (fun userId -> (userId, embassyId |> Graph.NodeIdValue))
            |> Result.map GetRequest.UserEmbassy
        | _ -> $"'{parts}' for Users.GetRequest" |> NotSupported |> Error

type Request =
    | Get of GetRequest

    member this.Route =
        match this with
        | Get r -> r.Code

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '0' -> parts |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' route of Users" |> NotSupported |> Error
