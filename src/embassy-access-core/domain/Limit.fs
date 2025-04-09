[<AutoOpen>]
module EA.Core.Domain.Limit

open System
open Infrastructure.Prelude
open Infrastructure.Domain

type private Refresh =
    | Ready
    | Waiting of TimeSpan

    static member validate remainingPeriod modifiedDate =
        match max (remainingPeriod - (DateTime.UtcNow - modifiedDate)) TimeSpan.Zero with
        | p when p = TimeSpan.Zero -> Ready
        | period -> Waiting period

type LimitState =
    | Valid of uint<attempts> * TimeSpan * DateTime
    | Invalid of TimeSpan * DateTime

    static member internal create attempts period =
        match attempts > 0u<attempts> && period > TimeSpan.Zero with
        | true -> Valid(attempts, period, DateTime.UtcNow)
        | false -> Invalid(period, DateTime.UtcNow)

    static member internal calculate attempts period =
        let attempts =
            match attempts > 0u<attempts> with
            | true -> attempts - 1u<attempts>
            | false -> 0u<attempts>

        LimitState.create attempts period

    static member internal validate attempts period date =
        match attempts > 0u<attempts> with
        | true -> Ok()
        | false ->
            match Refresh.validate period date with
            | Ready -> Ok()
            | Waiting period ->
                $"Limit of attempts reached. Remaining period: '%s{period |> String.fromTimeSpan}'."
                |> Error

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
        match limit.State with
        | Valid(attempts, period, date) ->
            match Refresh.validate period date with
            | Ready -> Limit.create (limit.Attempts - 1u<attempts>, limit.Period)
            | Waiting period -> {
                limit with
                    State = LimitState.calculate attempts period
              }
        | Invalid(period, date) ->
            match Refresh.validate period date with
            | Ready -> Limit.create (limit.Attempts - 1u<attempts>, limit.Period)
            | Waiting period -> {
                limit with
                    State = LimitState.calculate 0u<attempts> period
              }

    static member validate limit =
        match limit.State with
        | Valid(attempts, period, date) -> LimitState.validate attempts period date
        | Invalid(period, date) -> LimitState.validate 0u<attempts> period date
