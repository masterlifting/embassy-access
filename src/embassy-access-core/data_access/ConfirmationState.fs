module EA.Core.DataAccess.ConfirmationState

open System
open Infrastructure.Domain
open EA.Core.Domain

[<Literal>]
let private DISABLED = nameof Disabled

[<Literal>]
let private APPOINTMENT = nameof Appointment

[<Literal>]
let private FIRST_AVAILABLE = nameof FirstAvailable

[<Literal>]
let private LAST_AVAILABLE = nameof LastAvailable

[<Literal>]
let private DATE_TIME_RANGE = nameof DateTimeRange

type ConfirmationStateEntity() =

    member val Type = String.Empty with get, set
    member val AppointmentId: string option = None with get, set
    member val DateStart: Nullable<DateTime> = Nullable() with get, set
    member val DateEnd: Nullable<DateTime> = Nullable() with get, set

    member this.ToDomain() =
        match this.Type with
        | DISABLED -> Disabled |> Ok
        | APPOINTMENT ->
            match this.AppointmentId with
            | Some id -> id |> AppointmentId.parse |> Result.map Appointment
            | None -> $"{nameof AppointmentId} not found." |> NotFound |> Error
        | FIRST_AVAILABLE -> FirstAvailable |> Ok
        | LAST_AVAILABLE -> LastAvailable |> Ok
        | DATE_TIME_RANGE ->
            match this.DateStart |> Option.ofNullable, this.DateEnd |> Option.ofNullable with
            | Some min, Some max -> DateTimeRange(min, max) |> Ok
            | _ ->
                $"'{nameof this.DateStart}' or '{nameof this.DateEnd}' of '{nameof ConfirmationStateEntity}' not found."
                |> NotFound
                |> Error
        | _ ->
            $"The '%s{this.Type}' of '{nameof ConfirmationStateEntity}' is not supported."
            |> NotSupported
            |> Error

type internal ConfirmationState with
    member internal this.ToEntity() =
        let result = ConfirmationStateEntity()

        match this with
        | Disabled -> result.Type <- DISABLED
        | Appointment appointmentId ->
            result.Type <- APPOINTMENT
            result.AppointmentId <- Some appointmentId.ValueStr
        | FirstAvailable -> result.Type <- FIRST_AVAILABLE
        | LastAvailable -> result.Type <- LAST_AVAILABLE
        | DateTimeRange(min, max) ->
            result.Type <- DATE_TIME_RANGE
            result.DateStart <- Nullable min
            result.DateEnd <- Nullable max

        result
