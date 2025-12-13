module EA.Telegram.Features.Router.Services.Russian

open Infrastructure.Domain
open EA.Telegram.Shared
open EA.Telegram.Features.Router.Services.Russian

type Route =
    | Kdmid of Kdmid.Route
    | Midpass of Midpass.Route

    member this.Value =
        match this with
        | Kdmid r -> [ "0"; r.Value ]
        | Midpass r -> [ "1"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Kdmid.Route.parse |> Result.map Kdmid
        | "1" -> remaining |> Midpass.Route.parse |> Result.map Midpass
        | _ ->
            $"'{input}' of 'Services.Russian' endpoint is not supported."
            |> NotSupported
            |> Error
