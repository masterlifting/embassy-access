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
    | Russian of Services.Russian.Request

    member this.Route =
        match this with
        | Services r -> [ "0"; r.Route ]
        | Embassies r -> [ "1"; r.Route ]
        | Users r -> [ "2"; r.Route ]
        | Russian r -> [ "3"; r.Route ]
        |> String.concat Delimiter

    static member parse(input: string) =
        fun (deps: Core.Dependencies) ->
            let parts = input.Split Delimiter
            let remaining = parts[1..] |> String.concat Delimiter

            match parts[0] with
            | "0" -> remaining |> Services.Request.parse |> Result.map Services
            | "1" -> remaining |> Embassies.Request.parse |> Result.map Embassies
            | "2" -> remaining |> Users.Request.parse |> Result.map Users
            | "3" -> remaining |> Services.Russian.Request.parse |> Result.map Russian
            | "/start" -> Embassies(Embassies.Get(Embassies.Embassies)) |> Ok
            | "/mine" -> Users(Users.Get(Users.UserEmbassies(deps.ChatId))) |> Ok
            | _ -> $"'{parts}' route of Router" |> NotSupported |> Error
