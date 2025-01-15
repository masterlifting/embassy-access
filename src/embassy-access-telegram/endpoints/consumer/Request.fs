module EA.Telegram.Endpoints.Consumer.Request

open Infrastructure.Domain
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer.Embassies

[<Literal>]
let private Delimiter = "|"

type Route =
    | Users of Users.Request
    | Embassies of Embassies.Request
    | RussianEmbassy of RussianEmbassy.Request

    member this.Value =
        match this with
        | Users r -> [ "0"; r.Value ]
        | Embassies r -> [ "1"; r.Value ]
        | RussianEmbassy r -> [ "2"; r.Value ]
        |> String.concat Delimiter

    static member parse(input: string) =
        fun (deps: Consumer.Dependencies) ->
            let parts = input.Split Delimiter
            let remaining = parts[1..] |> String.concat Delimiter

            match parts[0] with
            | "0" -> remaining |> Users.Request.parse |> Result.map Users
            | "1" -> remaining |> Embassies.Request.parse |> Result.map Embassies
            | "2" -> remaining |> RussianEmbassy.Request.parse |> Result.map RussianEmbassy
            | "/start" -> Embassies(Embassies.Get(Embassies.GetRequest.Embassies)) |> Ok
            | "/mine" -> Users(Users.Get(Users.GetRequest.UserEmbassies(deps.ChatId))) |> Ok
            | _ -> $"'{input}' of Endpoints" |> NotSupported |> Error
