module EA.Telegram.Router.Embassies.Russian.Kdmid.Delete

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Route =
    | Subscription of RequestId

    member this.Value =
        match this with
        | Subscription requestId -> [ "0"; requestId.ValueStr ]
        |> String.concat Constants.Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Constants.Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> RequestId.parse requestId |> Result.map Subscription
        | _ ->
            $"'{parts}' of Embassies.Russian.Kdmid.Delete endpoint is not supported."
            |> NotSupported
            |> Error
