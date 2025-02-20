module EA.Telegram.Endpoints.Culture.Post

open Infrastructure.Domain
open EA.Telegram.Domain

type Request =
    | SetCulture of Culture
    | SetCultureCallback of string * Culture

    member this.Value =
        match this with
        | SetCulture culture -> [ "0"; culture.Value ]
        | SetCultureCallback(request, culture) -> [ "1"; request; culture.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts with
        | [| "0"; code |] -> code |> Culture.create |> SetCulture |> Ok
        | [| "1"; route; code |] -> (route, code |> Culture.create) |> SetCultureCallback |> Ok
        | _ -> $"'{parts}' of Culture.Post endpoint" |> NotSupported |> Error
