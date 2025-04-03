[<AutoOpen>]
module EA.Core.Domain.Limitation

open System
open Infrastructure.Domain

type LimitationState =
    | Active of TimeSpan * uint<attempts>
    | Inactive of TimeSpan
    | Expired

type Limitation = {
    Limit: uint<attempts>
    Period: TimeSpan
    State: LimitationState
} with

    member this.Check (date: DateTime) timeZone =

        let state =
            match this.State with
            | Active(period, limit) ->
                let now = DateTime.UtcNow.AddHours timeZone
                let lastModifiedDate = date.AddHours timeZone
                let remainingPeriod = period - (now - lastModifiedDate)
                let remainingLimit = limit - 1u<attempts>

                if remainingPeriod < TimeSpan.Zero then Expired
                else if remainingLimit = 0u<attempts> then Inactive remainingPeriod
                else Active(remainingPeriod, remainingLimit)
            | Expired -> Active(this.Period, this.Limit)
            | Inactive period -> Active(period, 0u<attempts>)
            

        //
        // match modified.DayOfYear = today.DayOfYear, attempt > attemptLimit with
        // | true, true ->
        //     Error
        //     <| Canceled $"Number of attempts reached the limit '%i{attemptLimit}' for today. The operation cancelled."
        // | true, false ->
        //     {
        //         request with
        //             Attempt = DateTime.UtcNow, attempt + 1
        //     }
        //     |> Ok
        // | _ ->
        //     {
        //         request with
        //             Attempt = DateTime.UtcNow, 1
        //     }
        //     |> Ok

        this |> Ok
