[<RequireQualifiedAccess>]
module EA.Telegram.Router.Embassies.Italian.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Embassies.Italian

type Route =
    | Prenotami of Prenotami.Get.Route

    member this.Value =
        match this with
        | Prenotami r -> [ "0"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Prenotami.Get.Route.parse |> Result.map Prenotami
        | _ ->
            $"'{parts}' of 'Embassies.Italian' endpoint is not supported."
            |> NotSupported
            |> Error
