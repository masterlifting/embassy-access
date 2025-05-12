module EA.Italian.Services.Router

open EA.Core.Domain
open Infrastructure.Domain

module Visa =

    type Route =
        | Tourism1 of string
        | Tourism2 of string

        member this.Value =
            match this with
            | Tourism1 op -> [ "0"; op ]
            | Tourism2 op -> [ "1"; op ]

    let parse (input: string list) =
        match input with
        | [ "0"; operation ] -> operation |> Tourism1 |> Ok
        | [ "1"; operation ] -> operation |> Tourism2 |> Ok
        | _ -> $"Visa route for the Italian router is not supported." |> NotSupported |> Error

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
        $"'%s{serviceId.ValueStr}' for the Italian router is not supported."
        |> NotSupported
        |> Error
