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
        | Embassy id -> [ "0"; id.ValueStr ]
        | Embassies -> [ "1" ]
        | UserEmbassy id -> [ "2"; id.ValueStr ]
        | UserEmbassies -> [ "3" ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; id |] -> id |> Tree.NodeIdValue |> EmbassyId |> Embassy |> Ok
        | [| "1" |] -> Embassies |> Ok
        | [| "2"; id |] -> id |> Tree.NodeIdValue |> EmbassyId |> UserEmbassy |> Ok
        | [| "3" |] -> UserEmbassies |> Ok
        | _ ->
            $"'{input}' of 'Embassies.Get' endpoint is not supported."
            |> NotSupported
            |> Error
