[<RequireQualifiedAccess>]
module EA.Telegram.Router.Subscriptions.Method

open Infrastructure.Domain
open EA.Telegram.Domain

type Route =
    | Get of Get.Route
    | Delete of Delete.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Delete r -> [ "1"; r.Value ]
        |> String.concat Router.DELIMITER

let parse (input: string) =
    let parts = input.Split Router.DELIMITER
    let remaining = parts[1..] |> String.concat Router.DELIMITER

    match parts[0] with
    | "0" -> remaining |> Get.Route.parse |> Result.map Get
    | "1" -> remaining |> Delete.Route.parse |> Result.map Delete
    | _ -> $"'{input}' of 'Subscriptions' endpoint is not supported." |> NotSupported |> Error
