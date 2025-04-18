﻿[<RequireQualifiedAccess>]
module EA.Telegram.Router.Services.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Services

type Route =
    | Russian of Russian.Method.Route
    | Italian of Italian.Method.Route

    member this.Value =
        match this with
        | Russian r -> [ "0"; r.Value ]
        | Italian r -> [ "1"; r.Value ]
        |> String.concat Router.DELIMITER

let parse (input: string) =
    let parts = input.Split Router.DELIMITER
    let remaining = parts[1..] |> String.concat Router.DELIMITER

    match parts[0] with
    | "0" -> remaining |> Russian.Method.Route.parse |> Result.map Russian
    | "1" -> remaining |> Italian.Method.Route.parse |> Result.map Italian
    | _ -> $"'{input}' of 'Services' endpoint is not supported." |> NotSupported |> Error
