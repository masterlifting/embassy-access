module EA.Telegram.Endpoints.Consumer.Request

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer

type Request =
    | Culture of Culture.Request.Request
    | Users of Users.Request.Request
    | Embassies of Embassies.Request.Request
    | RussianEmbassy of Embassies.Russian.Request.Request

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
        | "0" -> remaining |> Culture.Request.Request.parse |> Result.map Culture
        | "1" -> remaining |> Users.Request.Request.parse |> Result.map Users
        | "2" -> remaining |> Embassies.Request.Request.parse |> Result.map Embassies
        | "3" ->
            remaining
            |> Embassies.Russian.Request.Request.parse
            |> Result.map RussianEmbassy
        | "/culture" -> Culture(Culture.Request.Get(Culture.Get.Request.Cultures)) |> Ok
        | "/start" -> Embassies(Embassies.Request.Get(Embassies.Get.Request.Embassies)) |> Ok
        | "/mine" -> Users(Users.Request.Get(Users.Get.Request.UserEmbassies)) |> Ok
        | _ -> $"'{input}' of Endpoints.Consumer" |> NotSupported |> Error
