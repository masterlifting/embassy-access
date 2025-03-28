﻿module EA.Telegram.Router.Embassies.Russian.Post

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Embassies.Russian

type Route =
    | Kdmid of Kdmid.Post.Route

    member this.Value =
        match this with
        | Kdmid r -> [ "0"; r.Value ]
        |> String.concat Constants.Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Router.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Kdmid.Post.Route.parse |> Result.map Kdmid
        | _ ->
            $"'{parts}' of Embassies.Russian.Post endpoint is not supported."
            |> NotSupported
            |> Error
