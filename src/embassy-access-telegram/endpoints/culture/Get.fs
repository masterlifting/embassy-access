module EA.Telegram.Endpoints.Culture.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Request =
    | Cultures
    | CulturesCallback of string

    member this.Value =
        match this with
        | Cultures -> [ "0" ]
        | CulturesCallback request -> [ "1"; request ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts with
        | [| "0" |] -> Cultures |> Ok
        | [| "1"; r |] -> CulturesCallback r |> Ok
        | _ -> $"'{parts}' of Culture.Get endpoint" |> NotSupported |> Error
