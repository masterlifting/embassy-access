module EA.Telegram.Router.Embassies.Russian.Get

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Embassies.Russian

type Route =
    | Kdmid of Kdmid.Get.Route
    | Midpass of Midpass.Get.Route

    member this.Value =
        match this with
        | Kdmid r -> [ "0"; r.Value ]
        | Midpass r -> [ "1"; r.Value ]
        |> String.concat Constants.Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Router.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Kdmid.Get.Route.parse |> Result.map Kdmid
        | "1" -> remaining |> Midpass.Get.Route.parse |> Result.map Midpass
        | _ -> $"'{parts}' of Embassies.Russian.Get endpoint is not supported." |> NotSupported |> Error
