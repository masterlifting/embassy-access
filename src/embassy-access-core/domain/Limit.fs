[<AutoOpen>]
module EA.Core.Domain.Limit

open System
open Infrastructure.Prelude
open Infrastructure.Domain

type private Refresh =
    | Ready
    | Waiting of TimeSpan

    static member calculate remainingPeriod created =
        let updatedPeriod = remainingPeriod - (DateTime.UtcNow - created)
        let remainingPeriod = max updatedPeriod TimeSpan.Zero

        match remainingPeriod = TimeSpan.Zero with
        | true -> Ready
        | false -> Waiting remainingPeriod

type LimitState =
    | Valid of uint<attempts> * TimeSpan * DateTime
    | Invalid of TimeSpan * DateTime

    static member internal create attempts period =
        match period > TimeSpan.Zero && attempts > 0u<attempts> with
        | true -> Valid(attempts, period, DateTime.UtcNow)
        | false -> Invalid(period, DateTime.UtcNow)

type Limit = {
    Attempts: uint<attempts>
    Period: TimeSpan
    State: LimitState
} with

    static member init(attempts, period) = {
        Attempts = attempts
        Period = period
        State = LimitState.create attempts period
    }

    static member private create(attempts, period) = {
        Attempts = attempts
        Period = period
        State = LimitState.create (attempts - 1u<attempts>) period
    }

    static member update limit =
        match limit.State with
        | Valid(attempts, period, date) ->
            match Refresh.calculate period date with
            | Ready -> Limit.create (limit.Attempts, limit.Period)
            | Waiting period -> {
                limit with
                    State = LimitState.create (attempts - 1u<attempts>) period
              }
        | Invalid(period, date) ->
            match Refresh.calculate period date with
            | Ready -> Limit.create (limit.Attempts, limit.Period)
            | Waiting period -> {
                limit with
                    State = LimitState.create 0u<attempts> period
              }

    static member validate limit =
        match limit.State with
        | Valid _ -> Ok()
        | Invalid(period, date) ->
            match Refresh.calculate period date with
            | Ready -> Ok()
            | Waiting period ->
                $"Limit of attempts reached. Remaining period: '%s{period |> TimeSpan.print}'"
                |> Error

    static member print limit =
        match limit.State with
        | Valid(attempts, period, _) -> $"Remaining attempts '%i{attempts}' for '%s{period |> TimeSpan.print}'"
        | Invalid(period, date) ->
            match Refresh.calculate period date with
            | Ready -> $"Remaining attempts '%i{limit.Attempts}' for '%s{limit.Period |> TimeSpan.print}'"
            | Waiting period ->
                $"Unavailable now. '%i{limit.Attempts}' attempts will be available in '%s{period |> TimeSpan.print}'"
