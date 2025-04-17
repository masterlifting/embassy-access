[<RequireQualifiedAccess>]
module EA.Telegram.Router.Embassies.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Embassies

type Route =
    | Get of Get.Route
    | Russian of Russian.Method.Route
    | Italian of Italian.Method.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Russian r -> [ "1"; r.Value ]
        | Italian r -> [ "2"; r.Value ]
        |> String.concat Router.DELIMITER

let parse (input: string) =
    let parts = input.Split Router.DELIMITER
    let remaining = parts[1..] |> String.concat Router.DELIMITER

    match parts[0] with
    | "0" -> remaining |> Get.Route.parse |> Result.map Get
    | "1" -> remaining |> Russian.Method.Route.parse |> Result.map Russian
    | "2" -> remaining |> Italian.Method.Route.parse |> Result.map Italian
    | _ -> $"'{input}' of 'Embassies' endpoint is not supported." |> NotSupported |> Error
