module EA.Core.DataAccess.Limitation

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain

type LimitationEntity() =
    member val Count = 0u with get, set
    member val Period = String.Empty with get, set

    member this.ToDomain() =
        match this.Period with
        | AP.IsTimeSpan period ->
            {
                Count = this.Count * 1u<attempts>
                Period = period
            }
            |> Ok
        | _ -> $"Limitation '{this.Period}' is not supported." |> NotSupported |> Error
