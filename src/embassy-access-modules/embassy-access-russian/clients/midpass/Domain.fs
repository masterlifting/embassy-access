module EA.Russian.Clients.Domain.Midpass

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open System.Collections.Concurrent
open Web.Clients.Domain

type Client = Http.Client
type ClientFactory = ConcurrentDictionary<string, Client>

type Dependencies = { Number: string }

type Payload = {
    Value: int
} with

    static member create(payload: string) =
        match payload with
        | AP.IsInt id -> { Value = id } |> Ok
        | _ -> $"Midpass payload: '%s{payload}' is not supported." |> NotSupported |> Error

    static member print(payload: Payload) = $"'ID:%i{payload.Value}'"
