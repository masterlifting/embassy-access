module EA.Telegram.Router.Services.Russian.Midpass.Get

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | Print of RequestId
    | Menu of RequestId

    member this.Value =
        match this with
        | Print requestId -> [ "0"; requestId.ValueStr ]
        | Menu requestId -> [ "1"; requestId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> Print(requestId |> UUID16 |> RequestId) |> Ok
        | [| "1"; requestId |] -> Menu(requestId |> UUID16 |> RequestId) |> Ok
        | _ ->
            $"'{parts}' of 'Services.Russian.Midpass.Get' endpoint is not supported."
            |> NotSupported
            |> Error
