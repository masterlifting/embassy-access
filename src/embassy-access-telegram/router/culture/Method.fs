[<RequireQualifiedAccess>]
module EA.Telegram.Router.Culture.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Culture

type Route =
    | Get of Get.Route
    | Post of Post.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Post r -> [ "1"; r.Value ]
        |> String.concat Router.DELIMITER

let parse (input: string) =
    let parts = input.Split Router.DELIMITER
    let remaining = parts[1..] |> String.concat Router.DELIMITER

    match parts[0] with
    | "0" -> remaining |> Get.Route.parse |> Result.map Get
    | "1" -> remaining |> Post.Route.parse |> Result.map Post
    | _ -> $"'{input}' of Culture endpoint is not supported." |> NotSupported |> Error
