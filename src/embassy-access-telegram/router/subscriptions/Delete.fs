module EA.Telegram.Router.Subscriptions.Delete

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Route =
    | Subscription of RequestId

    member this.Value =
        match this with
        | Subscription requestId -> [ "0"; requestId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> RequestId.parse requestId |> Result.map Subscription
        | _ ->
            $"'{parts}' of 'Subscriptions.Delete' endpoint is not supported."
            |> NotSupported
            |> Error
