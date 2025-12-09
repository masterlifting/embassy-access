[<RequireQualifiedAccess>]
module EA.Telegram.Router.Router

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router
open EA.Telegram.Router.Services

type Route =
    | Culture of EA.Telegram.Features.Culture.Router.Method.Route
    | Services of Method.Route
    | Embassies of EA.Telegram.Features.Embassies.Router.Method.Route

    member this.Value =
        match this with
        | Culture r -> [ "0"; r.Value ]
        | Services r -> [ "1"; r.Value ]
        | Embassies r -> [ "2"; r.Value ]
        |> String.concat EA.Telegram.Domain.Constants.Router.DELIMITER

let parse (input: string) =
    let parts = input.Split EA.Telegram.Domain.Constants.Router.DELIMITER
    let remaining =
        parts[1..] |> String.concat EA.Telegram.Domain.Constants.Router.DELIMITER

    match parts[0] with
    | "0" ->
        remaining
        |> EA.Telegram.Features.Culture.Router.Method.parse
        |> Result.map Culture
    | "1" -> remaining |> Method.parse |> Result.map Services
    | "2" ->
        remaining
        |> EA.Telegram.Features.Embassies.Router.Method.parse
        |> Result.map Embassies
    | "/culture" ->
        Culture(EA.Telegram.Features.Culture.Router.Method.Get(EA.Telegram.Features.Culture.Router.Get.Route.Cultures))
        |> Ok
    | "/start" ->
        Embassies(
            EA.Telegram.Features.Embassies.Router.Method.Get(EA.Telegram.Features.Embassies.Router.Get.Route.Embassies)
        )
        |> Ok
    | "/mine" ->
        Embassies(
            EA.Telegram.Features.Embassies.Router.Method.Get(
                EA.Telegram.Features.Embassies.Router.Get.Route.UserEmbassies
            )
        )
        |> Ok
    | _ ->
        $"'{input}' for the application router is not supported."
        |> NotSupported
        |> Error
