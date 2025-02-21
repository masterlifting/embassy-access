module EA.Telegram.Endpoints.Culture.Post

open Infrastructure.Domain
open EA.Telegram.Domain

// There is required to define its own DELIMITER constant as we can have the callback with the same delimiter
[<Literal>]
let DELIMITER = "'"

type Request =
    | SetCulture of Culture
    | SetCultureCallback of string * Culture

    member this.Value =
        match this with
        | SetCulture culture -> [ "0"; culture.Value ]
        | SetCultureCallback(callback, culture) -> [ "1"; callback; culture.Value ]
        |> String.concat DELIMITER

    static member parse(input: string) =
        let parts = input.Split DELIMITER

        match parts with
        | [| "0"; code |] -> code |> Culture.create |> SetCulture |> Ok
        | [| "1"; route; code |] -> (route, code |> Culture.create) |> SetCultureCallback |> Ok
        | _ -> $"'{parts}' of Culture.Post endpoint" |> NotSupported |> Error
