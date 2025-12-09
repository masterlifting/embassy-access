module EA.Telegram.Router.Services.Italian.Prenotami.Get

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | Info of RequestId
    | Menu of RequestId

    member this.Value =
        match this with
        | Info requestId -> [ "0"; requestId.Value ]
        | Menu requestId -> [ "1"; requestId.Value ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> Info(requestId |> UUID16 |> RequestId) |> Ok
        | [| "1"; requestId |] -> Menu(requestId |> UUID16 |> RequestId) |> Ok
        | _ ->
            $"'{input}' of 'Services.Italian.Prenotami.Post' endpoint is not supported."
            |> NotSupported
            |> Error
