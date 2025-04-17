module EA.Telegram.Router.Subscriptions.Get

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Route =
    | Menu of RequestId

    member this.Value =
        match this with
        | Menu requestId -> [ "0"; requestId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> RequestId.parse requestId |> Result.map Menu
        | _ ->
            $"'{parts}' of 'Subscriptions.Get' endpoint is not supported."
            |> NotSupported
            |> Error
