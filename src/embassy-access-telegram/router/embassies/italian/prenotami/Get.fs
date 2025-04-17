﻿module EA.Telegram.Router.Embassies.Italian.Prenotami.Get

open Infrastructure.Domain
open EA.Telegram.Domain
open EA.Core.Domain

type Route =
    | Appointments of RequestId

    member this.Value =
        match this with
        | Appointments requestId -> [ "0"; requestId.ValueStr ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; requestId |] -> RequestId.parse requestId |> Result.map Appointments
        | _ ->
            $"'{parts}' of 'Embassies.Italian.Prenotami.Get' endpoint is not supported."
            |> NotSupported
            |> Error
