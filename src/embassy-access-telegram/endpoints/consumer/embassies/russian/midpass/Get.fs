﻿module EA.Telegram.Endpoints.Consumer.Embassies.Russian.Midpass.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Request =
    | Status of string

    member this.Value =
        match this with
        | Status number -> [ "0"; number ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts with
        | [| "0"; number |] -> Status number |> Ok
        | _ ->
            $"'{parts}' of Consumer.Embassies.Russian.Midpass.Get endpoint"
            |> NotSupported
            |> Error
