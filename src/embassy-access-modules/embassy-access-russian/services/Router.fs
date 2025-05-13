module EA.Russian.Services.Router

open EA.Core.Domain
open Infrastructure.Domain

module Operation =
    type Route =
        | CheckSlotsNow
        | SlotsAutoNotification
        | AutoBookingFirstSlot
        | AutoBookingFirstSlotInPeriod
        | AutoBookingLastSlot

        member this.Value =
            match this with
            | CheckSlotsNow -> "0"
            | SlotsAutoNotification -> "1"
            | AutoBookingFirstSlot -> "2"
            | AutoBookingFirstSlotInPeriod -> "3"
            | AutoBookingLastSlot -> "4"

    let parse (input: string) =
        match input with
        | "0" -> CheckSlotsNow |> Ok
        | "1" -> SlotsAutoNotification |> Ok
        | "2" -> AutoBookingFirstSlot |> Ok
        | "3" -> AutoBookingFirstSlotInPeriod |> Ok
        | "4" -> AutoBookingLastSlot |> Ok
        | _ ->
            "Service operation for the Russian router is not supported."
            |> NotSupported
            |> Error

module Passport =

    type Route =
        | International of Operation.Route
        | Status

        member this.Value =
            match this with
            | International op -> [ "0"; op.Value ]
            | Status -> [ "1" ]

    let parse (input: string list) =
        match input with
        | [ "0"; op ] -> op |> Operation.parse |> Result.map International
        | [ "1" ] -> Status |> Ok
        | _ ->
            "Passport service for the Russian router is not supported."
            |> NotSupported
            |> Error

module Notary =

    type Route =
        | PowerOfAttorney of Operation.Route

        member this.Value =
            match this with
            | PowerOfAttorney op -> [ "0"; op.Value ]

    let parse (input: string list) =
        match input with
        | [ "0"; op ] -> op |> Operation.parse |> Result.map PowerOfAttorney
        | _ ->
            "Notary service for the Russian router is not supported."
            |> NotSupported
            |> Error

module Citizenship =

    type Route =
        | Renunciation of Operation.Route

        member this.Value =
            match this with
            | Renunciation op -> [ "0"; op.Value ]

    let parse (input: string list) =
        match input with
        | [ "0"; op ] -> op |> Operation.parse |> Result.map Renunciation
        | _ ->
            "Citizenship service for the Russian router is not supported."
            |> NotSupported
            |> Error

type Route =
    | Passport of Passport.Route
    | Notary of Notary.Route
    | Citizenship of Citizenship.Route

    member this.Value =
        match this with
        | Passport r -> "0" :: r.Value
        | Notary r -> "1" :: r.Value
        | Citizenship r -> "2" :: r.Value

let parse (serviceId: ServiceId) =
    // Maybe I should make sure that the serviceId is an Russian serviceId
    let input = serviceId.Value |> Graph.NodeId.splitValues |> List.skip 2
    let remaining = input[1..]

    match input[0] with
    | "0" -> remaining |> Passport.parse |> Result.map Passport
    | "1" -> remaining |> Notary.parse |> Result.map Notary
    | "2" -> remaining |> Citizenship.parse |> Result.map Citizenship
    | _ ->
        $"'%s{serviceId.ValueStr}' for the Russian router is not supported."
        |> NotSupported
        |> Error
