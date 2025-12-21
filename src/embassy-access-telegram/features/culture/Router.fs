module EA.Telegram.Features.Router.Culture

open Infrastructure.Domain
open EA.Telegram.Shared

[<Literal>]
let ROOT = "culture"

type Get =
    | Cultures

    member this.Value =
        match this with
        | Cultures -> [ "0" ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0" |] -> Cultures |> Ok
        | _ ->
            $"'{parts}' of 'Culture.Get' endpoint is not supported."
            |> NotSupported
            |> Error

type Post =
    | SetCulture of Culture
    | SetCultureCallback of string * Culture

    member this.Value =
        match this with
        | SetCulture culture -> [ "0"; culture.Code ]
        | SetCultureCallback(callback, culture) -> [ "1"; callback; culture.Code ]
        |> String.concat "'"

    static member parse(input: string) =
        let parts = input.Split "'"

        match parts with
        | [| "0"; code |] -> code |> Culture.parse |> SetCulture |> Ok
        | [| "1"; route; code |] -> (route, code |> Culture.parse) |> SetCultureCallback |> Ok
        | _ ->
            $"'{parts}' of 'Culture.Post' endpoint is not supported."
            |> NotSupported
            |> Error

type Route =
    | Get of Get
    | Post of Post

    member this.Value =
        match this with
        | Get r -> [ ROOT; "0"; r.Value ]
        | Post r -> [ ROOT; "1"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.parse |> Result.map Get
        | "1" -> remaining |> Post.parse |> Result.map Post
        | _ -> $"'{input}' of 'Culture' endpoint is not supported." |> NotSupported |> Error
