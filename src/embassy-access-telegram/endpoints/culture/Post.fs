module EA.Telegram.Endpoints.Culture.Post

open Infrastructure.Domain
open EA.Telegram.Domain

module Model =
    type Culture = { Name: string; Code: string }

open Model

type Request =
    | SetCulture of Culture

    member this.Value =
        match this with
        | SetCulture model -> [ "0"; model.Name; model.Code ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts with
        | [| "0"; name; code |] -> { Name = name; Code = code } |> SetCulture |> Ok
        | _ ->
            $"'{parts}' of Culture.Post endpoint"
            |> NotSupported
            |> Error
