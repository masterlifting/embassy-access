module EA.Core.DataAccess.Limitation

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

[<Literal>]
let private START = nameof Start

[<Literal>]
let private ACTIVE = nameof Active

[<Literal>]
let private REACHED = nameof Reached

type LimitationEntity() =
    member val Limit = 0u with get, set
    member val Period = String.Empty with get, set
    member val StateType = String.Empty with get, set
    member val RemainingPeriod = String.Empty with get, set
    member val RemainingAttempts = 0u with get, set

    member this.ToDomain() =
        match this.Period with
        | AP.IsTimeSpan period ->
            match this.StateType with
            | START -> Start |> Ok
            | ACTIVE ->
                match this.RemainingPeriod with
                | AP.IsTimeSpan remainingPeriod -> Active(remainingPeriod, this.RemainingAttempts * 1u<attempts>) |> Ok
                | _ -> "Limitation 'RemainingPeriod' is not supported." |> NotSupported |> Error
            | REACHED ->
                match this.RemainingPeriod with
                | AP.IsTimeSpan remainingPeriod -> Reached remainingPeriod |> Ok
                | _ -> "Limitation 'RemainingPeriod' is not supported." |> NotSupported |> Error
            | _ -> $"Limitation 'StateType' is not supported." |> NotSupported |> Error
            |> Result.map (fun state -> {
                Limit = this.Limit * 1u<attempts>
                Period = period
                State = state
            })
        | _ -> $"Limitation '{this.Period}' is not supported." |> NotSupported |> Error

type internal Limitation with
    member this.ToEntity() =
        let entity =
            LimitationEntity(Limit = this.Limit / 1u<attempts>, Period = (this.Period |> string))

        match this.State with
        | Start -> entity.StateType <- START
        | Active(remainingPeriod, remainingAttempts) ->
            entity.StateType <- ACTIVE
            entity.RemainingPeriod <- remainingPeriod.ToString()
            entity.RemainingAttempts <- remainingAttempts / 1u<attempts>
        | Reached remainingPeriod ->
            entity.StateType <- REACHED
            entity.RemainingPeriod <- remainingPeriod.ToString()

        entity
