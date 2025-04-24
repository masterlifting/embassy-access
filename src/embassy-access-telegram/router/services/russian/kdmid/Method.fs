[<RequireQualifiedAccess>]
module EA.Telegram.Router.Services.Russian.Kdmid.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Services.Russian.Kdmid

type Route =
    | Post of Post.Route

    member this.Value =
        match this with
        | Post r -> [ "0"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "2" -> remaining |> Post.Route.parse |> Result.map Post
        | _ ->
            $"'{input}' of 'Services.Russian.Kdmid' endpoint is not supported."
            |> NotSupported
            |> Error
