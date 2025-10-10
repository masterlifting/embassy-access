module EA.Telegram.Router.Embassies.Get

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
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
