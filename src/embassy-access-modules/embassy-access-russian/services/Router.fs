module EA.Russian.Services.Router

open EA.Core.Domain
open Infrastructure.Domain

module Operation =
    module Immediate =
        type Route =
            | CheckSlots

            member this.Value =
                match this with
                | CheckSlots -> "0"

        let parse (input: string) =
            match input with
            | "0" -> CheckSlots |> Ok
            | _ ->
                "Immediate operation route for the Russian router is not supported."
                |> NotSupported
                |> Error

    module Background =

        type Route =
            | SlotsNotification
            | BookFirstSlot
            | BookFirstSlotInPeriod
            | BookLastSlot

            member this.Value =
                match this with
                | SlotsNotification -> "0"
                | BookFirstSlot -> "1"
                | BookLastSlot -> "2"
                | BookFirstSlotInPeriod -> "3"

        let parse (input: string) =
            match input with
            | "0" -> SlotsNotification |> Ok
            | "1" -> BookFirstSlot |> Ok
            | "2" -> BookLastSlot |> Ok
            | "3" -> BookFirstSlotInPeriod |> Ok
            | _ ->
                "Background operation route for the Russian router is not supported."
                |> NotSupported
                |> Error

    type Route =
        | Immediate of Immediate.Route
        | Background of Background.Route

        member this.Value =
            match this with
            | Immediate r -> [ "0"; r.Value ]
            | Background r -> [ "1"; r.Value ]

    let parse (input: string list) =
        match input[0] with
        | "0" -> input[1] |> Immediate.parse |> Result.map Immediate
        | "1" -> input[1] |> Background.parse |> Result.map Background
        | _ ->
            "Operation route for the Russian router is not supported."
            |> NotSupported
            |> Error

module Passport =

    type Route =
        | International of Operation.Route
        | Status

        member this.Value =
            match this with
            | International op -> "0" :: op.Value
            | Status -> [ "1" ]

    let parse (input: string list) =
        match input[0] with
        | "0" ->
            match input[1..] with
            | [] ->
                "Passport route for the Russian router is not supported."
                |> NotSupported
                |> Error
            | v -> v |> Operation.parse |> Result.map International
        | "1" -> Status |> Ok
        | _ ->
            $"Passport route for the Russian router is not supported."
            |> NotSupported
            |> Error

module Notary =

    type Route =
        | PowerOfAttorney of Operation.Route

        member this.Value =
            match this with
            | PowerOfAttorney op -> "0" :: op.Value

    let parse (input: string list) =
        match input[0] with
        | "0" ->
            match input[1..] with
            | [] -> "Notary route for the Russian router is not supported." |> NotSupported |> Error
            | v -> v |> Operation.parse |> Result.map PowerOfAttorney
        | _ ->
            $"Notary route for the Russian router is not supported."
            |> NotSupported
            |> Error

module Citizenship =

    type Route =
        | Renunciation of Operation.Route

        member this.Value =
            match this with
            | Renunciation op -> "0" :: op.Value

    let parse (input: string list) =
        match input[0] with
        | "0" ->
            match input[1..] with
            | [] ->
                "Citizenship route for the Russian router is not supported."
                |> NotSupported
                |> Error
            | v -> v |> Operation.parse |> Result.map Renunciation
        | _ ->
            "Citizenship route for the Russian router is not supported."
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
        $"'%s{input |> string}' for the Russian router is not supported."
        |> NotSupported
        |> Error
