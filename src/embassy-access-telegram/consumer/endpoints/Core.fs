module EA.Telegram.Consumer.Endpoints.Core

open Infrastructure.Domain
open EA.Telegram.Consumer.Dependencies

[<Literal>]
let private Delimiter = "|"

type Request =
    | Users of Users.Request
    | Embassies of Embassies.Request
    | RussianEmbassy of RussianEmbassy.Request

    member this.Route =
        match this with
        | Users r -> [ "0"; r.Route ]
        | Embassies r -> [ "1"; r.Route ]
        | RussianEmbassy r -> [ "2"; r.Route ]
        |> String.concat Delimiter

    static member parse(input: string) =
        fun (deps: Core.Dependencies) ->
            let parts = input.Split Delimiter
            let remaining = parts[1..] |> String.concat Delimiter

            match parts[0] with
            | "0" -> remaining |> Users.Request.parse |> Result.map Users
            | "1" -> remaining |> Embassies.Request.parse |> Result.map Embassies
            | "2" -> remaining |> RussianEmbassy.Request.parse |> Result.map RussianEmbassy
            | "/start" -> Embassies(Embassies.Get(Embassies.Embassies)) |> Ok
            | "/mine" -> Users(Users.Get(Users.UserEmbassies(deps.ChatId))) |> Ok
            | _ -> $"'{input}' of Endpoints" |> NotSupported |> Error
