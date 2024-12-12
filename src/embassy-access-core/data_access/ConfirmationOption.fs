module EA.Core.DataAccess.ConfirmationOption

open System
open Infrastructure.Domain
open EA.Core.Domain

[<Literal>]
let private FIRST_AVAILABLE = nameof FirstAvailable

[<Literal>]
let private LAST_AVAILABLE = nameof LastAvailable

[<Literal>]
let private DATE_TIME_RANGE = nameof DateTimeRange

type ConfirmationOptionEntity() =

    member val Type = String.Empty with get, set
    member val DateStart: Nullable<DateTime> = Nullable() with get, set
    member val DateEnd: Nullable<DateTime> = Nullable() with get, set

    member this.ToDomain() =
        match this.Type with
        | FIRST_AVAILABLE -> FirstAvailable |> Ok
        | LAST_AVAILABLE -> LastAvailable |> Ok
        | DATE_TIME_RANGE ->
            match this.DateStart |> Option.ofNullable, this.DateEnd |> Option.ofNullable with
            | Some min, Some max -> DateTimeRange(min, max) |> Ok
            | _ -> $"{nameof this.DateStart} or {nameof this.DateEnd}" |> NotFound |> Error
        | _ -> $"The %s{this.Type} of {nameof ConfirmationOptionEntity} " |> NotSupported |> Error

type internal ConfirmationOption with
    member internal this.ToEntity() =
        let result = ConfirmationOptionEntity()

        match this with
        | FirstAvailable -> result.Type <- FIRST_AVAILABLE
        | LastAvailable -> result.Type <- LAST_AVAILABLE
        | DateTimeRange(min, max) ->
            result.Type <- DATE_TIME_RANGE
            result.DateStart <- Nullable min
            result.DateEnd <- Nullable max

        result
