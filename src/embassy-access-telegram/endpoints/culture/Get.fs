module EA.Telegram.Endpoints.Culture.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Request =
    | Cultures

    member this.Value =
        match this with
        | Cultures -> [ "0" ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts with
        | [| "0" |] -> Cultures |> Ok
        | _ -> $"'{parts}' of Culture.Get endpoint" |> NotSupported |> Error
