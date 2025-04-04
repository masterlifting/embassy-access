[<AutoOpen>]
module EA.Core.Domain.Limitation

open System
open Infrastructure.Domain

type LimitationState =
    | New
    | Active of TimeSpan * uint<attempts>
    | Reached of TimeSpan

type Limitation = {
    Limit: uint<attempts>
    Period: TimeSpan
    State: LimitationState
}

let updateState (lastModifiedDate: DateTime) timeZone limitation =

    let state =
        match limitation.State with
        | New -> Active(limitation.Period, limitation.Limit - 1u<attempts>)
        | Active(period, limit) ->
            let currentDate = DateTime.UtcNow.AddHours timeZone
            let lastModifiedDate = lastModifiedDate.AddHours timeZone

            let remainingPeriod = period - (currentDate - lastModifiedDate)

            if remainingPeriod <= TimeSpan.Zero then
                Active(limitation.Period, limitation.Limit - 1u<attempts>)
            else if limit = 0u<attempts> then
                Reached remainingPeriod
            else
                Active(remainingPeriod, limit - 1u<attempts>)
        | Reached period -> Active(period, 0u<attempts>)

    { limitation with State = state }
