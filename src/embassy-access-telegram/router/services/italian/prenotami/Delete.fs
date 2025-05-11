module EA.Telegram.Router.Services.Italian.Prenotami.Delete

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | Subscription of RequestId

    member this.Value =
        match this with
        | Subscription requestId -> [ "0"; requestId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> Subscription(requestId |> UUID16 |> RequestId) |> Ok
        | _ ->
            $"'{input}' of 'Services.Italian.Prenotami.Delete' endpoint is not supported."
            |> NotSupported
            |> Error
