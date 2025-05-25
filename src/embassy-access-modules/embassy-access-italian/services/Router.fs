module EA.Italian.Services.Router

open EA.Core.Domain
open Infrastructure.Domain

module Prenotami =
    module Operation =
        type Route =
            | ManualRequest
            | AutoNotifications

            member this.Value =
                match this with
                | ManualRequest -> "0"
                | AutoNotifications -> "1"

        let parse (input: string) =
            match input with
            | "0" -> ManualRequest |> Ok
            | "1" -> AutoNotifications |> Ok
            | _ ->
                "Service operation for the Italian embassy is not supported."
                |> NotSupported
                |> Error

module Visa =

    type Route =
        | Tourism1 of Prenotami.Operation.Route
        | Tourism2 of Prenotami.Operation.Route

        member this.Value =
            match this with
            | Tourism1 op -> [ "0"; op.Value ]
            | Tourism2 op -> [ "1"; op.Value ]

    let parse (input: string list) =
        match input with
        | [ "0"; op ] -> op |> Prenotami.Operation.parse |> Result.map Tourism1
        | [ "1"; op ] -> op |> Prenotami.Operation.parse |> Result.map Tourism2
        | _ ->
            "Visa service for the Italian embassy is not supported."
            |> NotSupported
            |> Error

type Route =
    | Visa of Visa.Route

    member this.Value =
        match this with
        | Visa r -> "0" :: r.Value

let parse (serviceId: ServiceId) =
    // Maybe I should make sure that the serviceId is an Italian serviceId
    let input = serviceId.Value |> Graph.NodeId.splitValues |> List.skip 2
    let remaining = input[1..]

    match input[0] with
    | "0" -> remaining |> Visa.parse |> Result.map Visa
    | _ ->
        $"'%s{serviceId.ValueStr}' for the Italian embassy is not supported."
        |> NotSupported
        |> Error
