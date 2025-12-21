module EA.Telegram.Features.Router.Embassies

open Infrastructure.Domain
open EA.Telegram.Shared
open EA.Core.Domain

[<Literal>]
let ROOT = "embassies"

type Get =
    | Embassy of EmbassyId
    | Embassies
    | UserEmbassy of EmbassyId
    | UserEmbassies

    member this.Value =
        match this with
        | Embassy id -> [ "0"; id.Value ]
        | Embassies -> [ "1" ]
        | UserEmbassy id -> [ "2"; id.Value ]
        | UserEmbassies -> [ "3" ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; id |] -> id |> EmbassyId.create |> Result.map Embassy
        | [| "1" |] -> Embassies |> Ok
        | [| "2"; id |] -> id |> EmbassyId.create |> Result.map UserEmbassy
        | [| "3" |] -> UserEmbassies |> Ok
        | _ ->
            $"'{input}' of 'Embassies.Get' endpoint is not supported."
            |> NotSupported
            |> Error

type Route =
    | Get of Get

    member this.Value =
        match this with
        | Get r -> [ ROOT; "0"; r.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER
        let remaining = parts[1..] |> String.concat Router.DELIMITER

        match parts[0] with
        | "0" -> remaining |> Get.parse |> Result.map Get
        | _ -> $"'{input}' of 'Embassies' endpoint is not supported." |> NotSupported |> Error
