[<AutoOpen>]
module EA.Core.Domain.Limitation

open System
open Infrastructure.Domain

type LimitationState =
    | Start
    | Active of TimeSpan * uint<attempts>
    | Reached of TimeSpan

type Limitation = {
    Limit: uint<attempts>
    Period: TimeSpan
    State: LimitationState
}

let updateState (lastModifiedDate: DateTime) timeZone limitation =

    let calculateRemainingAttempts attempts =
        match attempts > 0u<attempts> with
        | true -> attempts - 1u<attempts>
        | false -> 0u<attempts>

    let calculateRemainingPeriod period =
        let currentDate = DateTime.UtcNow.AddHours timeZone
        let lastModifiedDate = lastModifiedDate.AddHours timeZone
        let remainingPeriod = period - (currentDate - lastModifiedDate)
        match remainingPeriod > TimeSpan.Zero with
        | true -> remainingPeriod
        | false -> TimeSpan.Zero

    let rec setState period attempts =
        let rec innerLoop count remainingAttempts remainingPeriod =
            match remainingAttempts = 0u<attempts>, remainingPeriod = TimeSpan.Zero with
            | true, false -> Reached remainingPeriod |> Ok
            | false, true -> innerLoop (count - 1) limitation.Limit limitation.Period
            | true, true ->
                match count > 0 with
                | true -> innerLoop (count - 1) limitation.Limit limitation.Period
                | false -> "Invalid state of limitation. The operation should be canceled." |> Canceled |> Error
            | false, false -> Active(remainingPeriod, remainingAttempts) |> Ok

        let remainingPeriod = calculateRemainingPeriod period
        let remainingAttempts = calculateRemainingAttempts attempts

        innerLoop 1 remainingAttempts remainingPeriod

    let state =
        match limitation.State with
        | Start -> setState limitation.Period limitation.Limit
        | Active(period, attempts) -> setState period attempts
        | Reached period -> setState period 0u<attempts>

    state |> Result.map (fun state -> { limitation with State = state })
