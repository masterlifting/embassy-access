﻿[<RequireQualifiedAccess>]
module EA.Telegram.Router.Users.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Users

type Method =
    | Get of Get.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ] |> String.concat Constants.Endpoint.DELIMITER

let parse (input: string) =
    let parts = input.Split Constants.Endpoint.DELIMITER
    let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

    match parts[0] with
    | "0" -> remaining |> Get.Route.parse |> Result.map Get
    | _ -> $"'{input}' of Users endpoint" |> NotSupported |> Error
