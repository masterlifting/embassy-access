[<RequireQualifiedAccess>]
module EA.Telegram.Router.Router

open Infrastructure.Domain
open EA.Telegram.Shared
open EA.Telegram.Features
open EA.Telegram.Features.Router

type Route =
    | Culture of Culture.Route
    | Services of Services.Router.Method.Route
    | Embassies of Embassies.Route

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
    | "0" -> remaining |> Culture.Route.parse |> Result.map Culture
    | "1" -> remaining |> Method.parse |> Result.map Services
    | "2" -> remaining |> Embassies.Route.parse |> Result.map Embassies
    | "/culture" -> Culture.Get Culture.Cultures |> Ok |> Result.map Culture
    | "/start" -> Embassies.Get Embassies.Embassies |> Ok |> Result.map Embassies
    | "/mine" -> Embassies.Get Embassies.UserEmbassies |> Ok |> Result.map Embassies
    | _ ->
        $"'{input}' for the application router is not supported."
        |> NotSupported
        |> Error
