module EA.Telegram.Endpoints.Culture.Post

open Infrastructure.Domain
open EA.Telegram.Domain

type Request =
    | SetCulture of string

    member this.Value =
        match this with
        | SetCulture code -> [ "0"; code ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts with
        | [| "0"; code |] -> code |> SetCulture |> Ok
        | _ -> $"'{parts}' of Culture.Post endpoint" |> NotSupported |> Error