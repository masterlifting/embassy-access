module EA.Core.DataAccess.Limit

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

[<Literal>]
let private VALID = nameof Valid

[<Literal>]
let private INVALID = nameof Invalid

type LimitEntity() =
    member val Attempts = 0u with get, set
    member val Period = String.Empty with get, set
    member val State = String.Empty with get, set
    member val StateDate = DateTime.MinValue with get, set
    member val RemainingPeriod = String.Empty with get, set
    member val RemainingAttempts = 0u with get, set

    member this.ToDomain() =
        match this.Period with
        | AP.IsTimeSpan period ->
            match this.State with
            | VALID ->
                match this.RemainingPeriod with
                | AP.IsTimeSpan remainingPeriod ->
                    Valid(this.RemainingAttempts * 1u<attempts>, remainingPeriod, this.StateDate)
                    |> Ok
                | _ -> "Limit 'RemainingPeriod' is not supported." |> NotSupported |> Error
            | INVALID ->
                match this.RemainingPeriod with
                | AP.IsTimeSpan remainingPeriod -> Invalid(remainingPeriod, this.StateDate) |> Ok
                | _ -> "Limit 'RemainingPeriod' is not supported." |> NotSupported |> Error
            | _ -> $"Limit 'State' is not supported." |> NotSupported |> Error
            |> Result.map (fun state -> {
                Attempts = this.Attempts * 1u<attempts>
                Period = period
                State = state
            })
        | _ -> $"Limit '{this.Period}' is not supported." |> NotSupported |> Error

type internal Limit with
    member this.ToEntity() =
        let entity =
            LimitEntity(Attempts = this.Attempts / 1u<attempts>, Period = (this.Period |> String.fromTimeSpan))

        match this.State with
        | Valid(remainingAttempts, remainingPeriod, date) ->
            entity.State <- VALID
            entity.StateDate <- date
            entity.RemainingPeriod <- remainingPeriod |> String.fromTimeSpan
            entity.RemainingAttempts <- remainingAttempts / 1u<attempts>
        | Invalid(remainingPeriod, date) ->
            entity.State <- INVALID
            entity.StateDate <- date
            entity.RemainingPeriod <- remainingPeriod |> String.fromTimeSpan

        entity
