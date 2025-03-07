module EA.Telegram.Endpoints.Embassies.Russian.Kdmid.Get

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Request =
    | Appointments of RequestId
    | SubscriptionsMenu of RequestId

    member this.Value =
        match this with
        | Appointments requestId -> [ "0"; requestId.ValueStr ]
        | SubscriptionsMenu requestId -> [ "1"; requestId.ValueStr ]
        |> String.concat Constants.Endpoint.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Endpoint.DELIMITER

        match parts with
        | [| "0"; requestId |] -> RequestId.create requestId |> Result.map Appointments
        | [| "1"; requestId |] -> RequestId.create requestId |> Result.map SubscriptionsMenu
        | _ -> $"'{parts}' of Embassies.Russian.Kdmid.Get endpoint" |> NotSupported |> Error
