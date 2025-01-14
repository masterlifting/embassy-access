module EA.Telegram.Endpoints.Consumer.Request

open Infrastructure.Domain
open EA.Telegram.Dependencies
open EA.Telegram.Endpoints.Consumer

[<Literal>]
let private Delimiter = "|"

type Route =
    | Users of Users.Request
    | Embassies of Embassies.Core.Request
    | RussianEmbassy of Embassies.Russian.Request

    member this.Value =
        match this with
        | Users r -> [ "0"; r.Value ]
        | Embassies r -> [ "1"; r.Value ]
        | RussianEmbassy r -> [ "2"; r.Value ]
        |> String.concat Delimiter

    static member parse(input: string) =
        fun (deps: Consumer.Core.Dependencies) ->
            let parts = input.Split Delimiter
            let remaining = parts[1..] |> String.concat Delimiter

            match parts[0] with
            | "0" -> remaining |> Users.Request.parse |> Result.map Users
            | "1" -> remaining |> Embassies.Core.Request.parse |> Result.map Embassies
            | "2" -> remaining |> Embassies.Russian.Request.parse |> Result.map RussianEmbassy
            | "/start" -> Embassies(Embassies.Core.Get(Embassies.Core.GetRequest.Embassies)) |> Ok
            | "/mine" -> Users(Users.Get(Users.GetRequest.UserEmbassies(deps.ChatId))) |> Ok
            | _ -> $"'{input}' of Endpoints" |> NotSupported |> Error
