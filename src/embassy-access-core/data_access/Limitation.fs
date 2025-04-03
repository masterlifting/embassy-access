module EA.Core.DataAccess.Limitation

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

type LimitationEntity() =
    member val Limit = 0u with get, set
    member val Period = String.Empty with get, set

    member this.ToDomain() =
        match this.Period with
        | AP.IsTimeSpan period ->
            {
                Limit = this.Limit * 1u<attempts>
                Period = period
            }
            |> Ok
        | _ -> $"Limitation '{this.Period}' is not supported." |> NotSupported |> Error

type internal Limitation with
    member this.ToEntity() =
        LimitationEntity(Limit = this.Limit / 1u<attempts>, Period = (this.Period |> string))
