[<RequireQualifiedAccess>]
module EA.Telegram.Router.Embassies.Russian.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Embassies.Russian

type Route =
    | Get of Get.Route
    | Post of Post.Route
    | Delete of Delete.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Post r -> [ "1"; r.Value ]
        | Delete r -> [ "2"; r.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

let parse (input: string) =
    let parts = input.Split Constants.Endpoint.DELIMITER
    let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

    match parts[0] with
    | "0" -> remaining |> Get.Route.parse |> Result.map Get
    | "1" -> remaining |> Post.Route.parse |> Result.map Post
    | "2" -> remaining |> Delete.Route.parse |> Result.map Delete
    | _ -> $"'{input}' of Embassies.Russian endpoint" |> NotSupported |> Error
