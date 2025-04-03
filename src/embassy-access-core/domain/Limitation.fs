[<AutoOpen>]
module EA.Core.Domain.Limitation

open System
open Infrastructure.Domain

type LimitationState =
    | Active of DateTime
    | Inactive
    | Expired
    | Reset

type Limitation = {
    Limit: uint<attempts>
    Period: TimeSpan
} with

    member this.Check (modified: DateTime) timeZone =

        let modified = modified.AddHours timeZone
        let today = DateTime.UtcNow.AddHours timeZone
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
