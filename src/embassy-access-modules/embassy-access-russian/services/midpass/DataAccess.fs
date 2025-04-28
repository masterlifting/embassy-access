module EA.Russian.Services.DataAccess.Midpass

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Russian.Services.Domain.Midpass

let private result = ResultBuilder()

[<RequireQualifiedAccess>]
module Payload =
    type Entity() =
        member val Number = 0 with get, set
        member val State = String.Empty with get, set

        member this.ToDomain() =
            result {
                let! number =
                    match this.Number with
                    | 0 -> $"Number '{this.Number}' is not supported." |> NotSupported |> Error
                    | number -> number |> Ok

                let! state =
                    match this.State with
                    | "" -> $"State '{this.State}' is not supported." |> NotSupported |> Error
                    | state -> state |> Some |> Ok

                return { Number = number; State = state }
            }

type Payload with

    static member toDomain(payload: Payload.Entity) = payload.ToDomain()

    static member toEntity(payload: Payload) =
        let state = payload.State |> Option.defaultValue String.Empty
        Payload.Entity(Number = payload.Number, State = state) |> Ok
