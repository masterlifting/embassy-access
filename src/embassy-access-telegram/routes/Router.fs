[<RequireQualifiedAccess>]
module EA.Telegram.Routes.Router

open Infrastructure.Domain
open EA.Telegram.Dependencies.Consumer

[<Literal>]
let private Delimiter = "|"

type Request =
    | Services of Services.Request
    | Embassies of Embassies.Request
    | Users of Users.Request

    member this.Route =
        match this with
        | Services r -> "0" + Delimiter + r.Route
        | Embassies r -> "1" + Delimiter + r.Route
        | Users r -> "2" + Delimiter + r.Route

    static member parse(input: string) =
        fun (deps: Core.Dependencies) ->
            let parts = input.Split Delimiter
            let remaining = parts[1..] |> String.concat Delimiter

            match parts[0] with
            | "0" -> remaining |> Services.Request.parse |> Result.map Services
            | "1" -> remaining |> Embassies.Request.parse |> Result.map Embassies
            | "2" -> remaining |> Users.Request.parse |> Result.map Users
            | "/start" -> Embassies(Embassies.Get(Embassies.All)) |> Ok
            | "/mine" -> Users(Users.Get(Users.Embassies((deps.ChatId, Embassies.All)))) |> Ok
            | _ -> $"'{parts}' route of Router" |> NotSupported |> Error
