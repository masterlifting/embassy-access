module EA.Telegram.Features.Router.Services.Italian

open Infrastructure.Domain
open EA.Telegram.Shared
open EA.Telegram.Features.Router.Services.Italian

type Route =
    | Prenotami of Prenotami.Route

    member this.Value =
        match this with
        | Prenotami r -> [ "0"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Prenotami.Route.parse |> Result.map Prenotami
        | _ ->
            $"'{input}' of 'Services.Italian' endpoint is not supported."
            |> NotSupported
            |> Error
