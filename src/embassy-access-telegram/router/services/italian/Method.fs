[<RequireQualifiedAccess>]
module EA.Telegram.Router.Services.Italian.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Services
open EA.Telegram.Router.Services.Italian

type Route =
    | Get of Get.Route
    | Prenotami of Prenotami.Get.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ]
        | Prenotami r -> [ "1"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.Route.parse |> Result.map Get
        | "1" -> remaining |> Prenotami.Get.Route.parse |> Result.map Prenotami
        | _ ->
            $"'{parts}' of 'Services.Italian' endpoint is not supported."
            |> NotSupported
            |> Error
