module EA.Telegram.Router.Embassies.Russian.Kdmid.Get

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Route =
    | Appointments of RequestId
    | SubscriptionsMenu of RequestId

    member this.Value =
        match this with
        | Appointments requestId -> [ "0"; requestId.ValueStr ]
        | SubscriptionsMenu requestId -> [ "1"; requestId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> RequestId.parse requestId |> Result.map Appointments
        | [| "1"; requestId |] -> RequestId.parse requestId |> Result.map SubscriptionsMenu
        | _ ->
            $"'{parts}' of Embassies.Russian.Kdmid.Get endpoint is not supported."
            |> NotSupported
            |> Error
