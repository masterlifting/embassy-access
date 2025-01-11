module EA.Telegram.Endpoints.Consumer.Users

open Infrastructure.Domain
open Web.Telegram.Domain

[<Literal>]
let private Delimiter = "|"

type GetRequest =
    | UserEmbassy of userId: ChatId * embassyId: Graph.NodeId
    | UserEmbassies of ChatId
    | UserEmbassyServices of ChatId * embassyId: Graph.NodeId
    | UserEmbassyService of userId: ChatId * embassyId: Graph.NodeId * serviceId: Graph.NodeId

    member this.Value =
        match this with
        | UserEmbassy(userId, embassyId) -> [ "00"; userId.ValueStr; embassyId.Value ]
        | UserEmbassies id -> [ "01"; id.ValueStr ]
        | UserEmbassyServices(userId, embassyId) -> [ "02"; userId.ValueStr; embassyId.Value ]
        | UserEmbassyService(userId, embassyId, serviceId) ->
            [ "03"; userId.ValueStr; embassyId.Value; serviceId.Value ]
        |> String.concat Delimiter

    static member parse(parts: string[]) =
        match parts with
        | [| "00"; userId; embassyId |] ->
            userId
            |> ChatId.parse
            |> Result.map (fun userId -> (userId, embassyId |> Graph.NodeIdValue))
            |> Result.map GetRequest.UserEmbassy
        | [| "01"; id |] -> id |> ChatId.parse |> Result.map GetRequest.UserEmbassies
        | [| "02"; userId; embassyId |] ->
            userId
            |> ChatId.parse
            |> Result.map (fun userId -> (userId, embassyId |> Graph.NodeIdValue))
            |> Result.map GetRequest.UserEmbassyServices
        | [| "03"; userId; embassyId; serviceId |] ->
            userId
            |> ChatId.parse
            |> Result.map (fun userId -> (userId, embassyId |> Graph.NodeIdValue, serviceId |> Graph.NodeIdValue))
            |> Result.map GetRequest.UserEmbassyService
        | _ -> $"'{parts}' of Users.GetRequest endpoint" |> NotSupported |> Error

type Request =
    | Get of GetRequest

    member this.Value =
        match this with
        | Get r -> r.Value

    static member parse(input: string) =
        let parts = input.Split Delimiter

        match parts[0][0] with
        | '0' -> parts |> GetRequest.parse |> Result.map Get
        | _ -> $"'{input}' of Users endpoint" |> NotSupported |> Error
