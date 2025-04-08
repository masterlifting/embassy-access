[<AutoOpen>]
module EA.Core.Domain.Limit

open System
open Infrastructure.Domain

type LimitState =
    | Valid of uint<attempts> * TimeSpan * DateTime
    | Invalid of TimeSpan * DateTime

    static member internal create attempts period =
        match attempts > 0u<attempts> && period > TimeSpan.Zero with
        | true -> Valid(attempts, period, DateTime.UtcNow)
        | false -> Invalid(period, DateTime.UtcNow)

type Limit = {
    Attempts: uint<attempts>
    Period: TimeSpan
    State: LimitState
} with

    static member create(attempts, period) = {
        Attempts = attempts
        Period = period
        State = LimitState.create attempts period
    }

    static member update limit =

        let calculateState attempts period previousDate =
            let attempts =
                match attempts > 0u<attempts> with
                | true -> attempts - 1u<attempts>
                | false -> 0u<attempts>

            let period = max (period - (DateTime.UtcNow - previousDate)) TimeSpan.Zero

            LimitState.create attempts period

        match limit.State with
        | Valid(attempts, period, date) -> {
            limit with
                State = calculateState attempts period date
          }
        | Invalid(period, date) ->
            match period > TimeSpan.Zero with
            | true -> {
                limit with
                    State = calculateState 0u<attempts> period date
              }
            | false -> Limit.create (limit.Attempts, limit.Period)
