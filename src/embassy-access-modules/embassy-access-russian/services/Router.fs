module EA.Russian.Services.Router

open EA.Core.Domain
open Infrastructure.Domain

module Passport =

    type Route =
        | International of string list
        | Status

        member this.Value =
            match this with
            | International ops -> "0" :: ops
            | Status -> [ "1" ]

    let parse (input: string list) =
        match input[0] with
        | "0" ->
            match input[1..] with
            | [] ->
                "Passport route for the Russian router is not supported."
                |> NotSupported
                |> Error
            | operations -> operations |> International |> Ok
        | "1" -> Status |> Ok
        | _ ->
            $"Passport route for the Russian router is not supported."
            |> NotSupported
            |> Error

module Notary =

    type Route =
        | PowerOfAttorney of string list

        member this.Value =
            match this with
            | PowerOfAttorney ops -> "0" :: ops

    let parse (input: string list) =
        match input[0] with
        | "0" ->
            match input[1..] with
            | [] -> "Notary route for the Russian router is not supported." |> NotSupported |> Error
            | operations -> operations |> PowerOfAttorney |> Ok
        | _ ->
            $"Notary route for the Russian router is not supported."
            |> NotSupported
            |> Error

module Citizenship =

    type Route =
        | Renunciation of string list

        member this.Value =
            match this with
            | Renunciation ops -> "0" :: ops

    let parse (input: string list) =
        match input[0] with
        | "0" ->
            match input[1..] with
            | [] ->
                "Citizenship route for the Russian router is not supported."
                |> NotSupported
                |> Error
            | operations -> operations |> Renunciation |> Ok
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
