[<RequireQualifiedAccess>]
module EA.Telegram.Router.Router

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router

type Route =
    | Culture of Culture.Method.Method
    | Users of Users.Method.Method
    | Embassies of Embassies.Method.Method
    | RussianEmbassy of Embassies.Russian.Method.Method

    member this.Value =
        match this with
        | Culture r -> [ "0"; r.Value ]
        | Users r -> [ "1"; r.Value ]
        | Embassies r -> [ "2"; r.Value ]
        | RussianEmbassy r -> [ "3"; r.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Culture.Method.Method.parse |> Result.map Culture
        | "1" -> remaining |> Users.Method.Method.parse |> Result.map Users
        | "2" -> remaining |> Embassies.Method.Method.parse |> Result.map Embassies
        | "3" -> remaining |> Embassies.Russian.Method.Method.parse |> Result.map RussianEmbassy
        | "/culture" -> Culture(Culture.Method.Get(Culture.Get.Route.Cultures)) |> Ok
        | "/mine" -> Users(Users.Method.Get(Users.Get.Route.UserEmbassies)) |> Ok
        | "/start" -> Embassies(Embassies.Method.Get(Embassies.Get.Route.Embassies)) |> Ok
        | _ -> $"'{input}' for the application" |> NotSupported |> Error
