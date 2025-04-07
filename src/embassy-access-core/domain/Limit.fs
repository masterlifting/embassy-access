[<AutoOpen>]
module EA.Core.Domain.Limit

open System
open Infrastructure.Domain

type Limit = {
    Attempts: uint<attempts>
    Period: TimeSpan
    State: LimitState
}
and LimitState =
    | Active of Limit
    | ReadyForRefresh
    | WaitingForActive of TimeSpan

let create (limit: Limit) =
    match limit.Attempts > 0u<attempts>, limit.Period > TimeSpan.Zero with
    | true, true -> Active { Attempts = limit.Attempts; Period = limit.Period } |> Ok
    | true, false -> Active { Attempts = limit.Attempts; Period = TimeSpan.Zero } |> Ok
    | false, true -> "Invalid attempts count." |> NotSupported |> Error
    | false, false -> "Invalid attempts count and period." |> NotSupported |> Error

let validate limit =
    match limit.Attempts > 0u<attempts>, limit.Period > TimeSpan.Zero with
    | true, true -> Active limit
    | true, false -> ReadyForRefresh limit
    | false, true -> WaitingForActive limit.Period
    | false, false -> ReadyForRefresh limit

let update (date: DateTime) timeZone limit =

    let calculateRemainingLimit (limit: Limit)=
        let attempts =
            match limit.Attempts > 0u<attempts> with
            | true -> limit.Attempts - 1u<attempts>
            | false -> 0u<attempts>

        let currentDate = DateTime.UtcNow.AddHours timeZone
        let lastModifiedDate = date.AddHours timeZone
        let remainingPeriod = limit.Period - (currentDate - lastModifiedDate)
        match remainingPeriod > TimeSpan.Zero with
        | true -> remainingPeriod
        | false -> TimeSpan.Zero
        
        { Attempts = attempts; Period = remainingPeriod }

    match limit |> validate with
    | Active limit -> limit |> calculateRemainingLimit |> Ok
    | WaitingForActive period 
