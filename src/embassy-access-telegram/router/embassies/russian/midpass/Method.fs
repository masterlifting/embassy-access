[<RequireQualifiedAccess>]
module EA.Telegram.Router.Embassies.Russian.Midpass.Method

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Telegram.Router.Embassies.Russian.Midpass

type Method =
    | Get of Get.Route

    member this.Value =
        match this with
        | Get r -> [ "0"; r.Value ] |> String.concat Constants.Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Router.DELIMITER
        let remaining = parts[1..] |> String.concat Constants.Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.Route.parse |> Result.map Get
        | _ ->
            $"'{input}' of Embassies.Russian.Midpass endpoint is not supported."
            |> NotSupported
            |> Error
