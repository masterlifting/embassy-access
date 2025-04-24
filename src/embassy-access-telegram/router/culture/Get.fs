module EA.Telegram.Router.Culture.Get

open Infrastructure.Domain
open EA.Telegram.Domain

type Route =
    | Cultures

    member this.Value =
        match this with
        | Cultures -> [ "0" ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0" |] -> Cultures |> Ok
        | _ ->
            $"'{parts}' of 'Culture.Get' endpoint is not supported."
            |> NotSupported
            |> Error
