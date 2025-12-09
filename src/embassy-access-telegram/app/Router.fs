[<RequireQualifiedAccess>]
module EA.Telegram.Router.Router

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router
open EA.Telegram.Features
open EA.Telegram.Router.Services

type Route =
    | Culture of Culture.Router.Method.Route
    | Services of Services.Router.Method.Route
    | Embassies of Embassies.Router.Method.Route

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
    | "0" -> remaining |> Culture.Router.Method.parse |> Result.map Culture
    | "1" -> remaining |> Method.parse |> Result.map Services
    | "2" -> remaining |> Embassies.Router.Method.parse |> Result.map Embassies
    | "/culture" -> Culture(Culture.Router.Method.Get Culture.Router.Get.Route.Cultures) |> Ok
    | "/start" ->
        Embassies(Embassies.Router.Method.Get Embassies.Router.Get.Route.Embassies)
        |> Ok
    | "/mine" ->
        Embassies(Embassies.Router.Method.Get Embassies.Router.Get.Route.UserEmbassies)
        |> Ok
    | _ ->
        $"'{input}' for the application router is not supported."
        |> NotSupported
        |> Error
