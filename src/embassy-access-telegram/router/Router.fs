[<RequireQualifiedAccess>]
module EA.Telegram.Router.Router

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router

type Route =
    | Culture of Culture.Method.Route
    | Services of Services.Method.Route
    | Embassies of Embassies.Method.Route

    member this.Value =
        match this with
        | Culture r -> [ "0"; r.Value ]
        | Services r -> [ "1"; r.Value ]
        | Embassies r -> [ "2"; r.Value ]
        |> String.concat Router.DELIMITER

let parse (input: string) =
    let parts = input.Split Router.DELIMITER
    let remaining = parts[1..] |> String.concat Router.DELIMITER

    match parts[0] with
    | "0" -> remaining |> Culture.Method.parse |> Result.map Culture
    | "1" -> remaining |> Services.Method.parse |> Result.map Services
    | "2" -> remaining |> Embassies.Method.parse |> Result.map Embassies
    | "/culture" -> Culture(Culture.Method.Get(Culture.Get.Route.Cultures)) |> Ok
    | "/start" -> Embassies(Embassies.Method.Get(Embassies.Get.Route.Embassies)) |> Ok
    | "/mine" -> Embassies(Embassies.Method.Get(Embassies.Get.Route.UserEmbassies)) |> Ok
    | _ ->
        $"'{input}' for the application router is not supported."
        |> NotSupported
        |> Error
