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
    let currentDate = DateTime.UtcNow.AddHours timeZone
    let lastModifiedDate = lastModifiedDate.AddHours timeZone
    let elapsed = currentDate - lastModifiedDate

    let state =
        match limitation.State with
        | Start -> Active(limitation.Period, limitation.Limit - 1u<attempts>)
        | Active(period, limit) ->
            let remainingPeriod = period - elapsed
            if remainingPeriod <= TimeSpan.Zero then
                Active(limitation.Period, limitation.Limit - 1u<attempts>)
            elif limit = 0u<attempts> then
                Reached remainingPeriod
            else
                Active(remainingPeriod, limit - 1u<attempts>)
        | Reached period ->
            let remainingPeriod = period - elapsed
            if remainingPeriod <= TimeSpan.Zero then
                Active(limitation.Period, limitation.Limit - 1u<attempts>)
            else
                Reached remainingPeriod

    { limitation with State = state }
