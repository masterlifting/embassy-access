module EA.Telegram.Endpoints.Consumer.Router

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer

type Request =
    | Users of Users.Request.Request
    | Embassies of Embassies.Request.Request
    | RussianEmbassy of Embassies.Russian.Request.Request

    member this.Value =
        match this with
        | Users r -> [ "0"; r.Value ]
        | Embassies r -> [ "1"; r.Value ]
        | RussianEmbassy r -> [ "2"; r.Value ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Endpoint.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Users.Request.Request.parse |> Result.map Users
        | "1" -> remaining |> Embassies.Request.Request.parse |> Result.map Embassies
        | "2" ->
            remaining
            |> Embassies.Russian.Request.Request.parse
            |> Result.map RussianEmbassy
        | "/start" -> Embassies(Embassies.Request.Get(Embassies.Get.Request.Embassies)) |> Ok
        | "/mine" -> Users(Users.Request.Get(Users.Get.Request.UserEmbassies)) |> Ok
        | _ -> $"'{input}' of Endpoints" |> NotSupported |> Error
