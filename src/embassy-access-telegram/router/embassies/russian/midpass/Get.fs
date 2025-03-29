module EA.Telegram.Router.Embassies.Russian.Midpass.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Route =
    | Status of string

    member this.Value =
        match this with
        | Status number -> [ "0"; number ]
        |> String.concat Constants.Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Router.DELIMITER

        match parts with
        | [| "0"; number |] -> Status number |> Ok
        | _ ->
            $"'{parts}' of Embassies.Russian.Midpass.Get endpoint is not supported."
            |> NotSupported
            |> Error
