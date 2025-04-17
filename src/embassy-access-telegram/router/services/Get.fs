module EA.Telegram.Router.Services.Get

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Route =
    | Service of ServiceId
    | Services
    | UserService of ServiceId
    | UserServices

    member this.Value =
        match this with
        | Service id -> [ "0"; id.ValueStr ]
        | Services -> [ "1" ]
        | UserService id -> [ "2"; id.ValueStr ]
        | UserServices -> [ "3" ]
        |> String.concat Router.DELIMITER

    static member parse(input: string) =
        let parts = input.Split Router.DELIMITER

        match parts with
        | [| "0"; id |] -> id |> Graph.NodeIdValue |> ServiceId |> Service |> Ok
        | [| "1" |] -> Services |> Ok
        | [| "2"; id |] -> id |> Graph.NodeIdValue |> ServiceId |> UserService |> Ok
        | [| "3" |] -> UserServices |> Ok
        | _ -> $"'{parts}' of 'Services.Get' endpoint is not supported." |> NotSupported |> Error
