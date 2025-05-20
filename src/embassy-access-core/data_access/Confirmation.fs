module EA.Core.DataAccess.Confirmation

open System
open Infrastructure.Domain
open EA.Core.Domain

[<Literal>]
let private DISABLED = nameof Disabled

[<Literal>]
let private APPOINTMENT = nameof ForAppointment

[<Literal>]
let private FIRST_AVAILABLE = nameof FirstAvailable

[<Literal>]
let private FIRS_AVAILABLE_IN_PERIOD = nameof FirstAvailableInPeriod

[<Literal>]
let private LAST_AVAILABLE = nameof LastAvailable

type ConfirmationEntity() =

    member val Type = String.Empty with get, set
    member val AppointmentId: string option = None with get, set
    member val DateStart: Nullable<DateTime> = Nullable() with get, set
    member val DateEnd: Nullable<DateTime> = Nullable() with get, set

    member this.ToDomain() =
        match this.Type with
        | DISABLED -> Disabled |> Ok
        | APPOINTMENT ->
            match this.AppointmentId with
            | Some id -> id |> AppointmentId.parse |> Result.map ForAppointment
            | None -> $"{nameof AppointmentId} not found." |> NotFound |> Error
        | FIRST_AVAILABLE -> FirstAvailable |> Ok
        | LAST_AVAILABLE -> LastAvailable |> Ok
        | FIRS_AVAILABLE_IN_PERIOD ->
            match this.DateStart |> Option.ofNullable, this.DateEnd |> Option.ofNullable with
            | Some min, Some max -> FirstAvailableInPeriod(min, max) |> Ok
            | _ ->
                $"'{nameof this.DateStart}' or '{nameof this.DateEnd}' of '{nameof ConfirmationEntity}' not found."
                |> NotFound
                |> Error
        | _ ->
            $"The '%s{this.Type}' of '{nameof ConfirmationEntity}' is not supported."
            |> NotSupported
            |> Error

type internal Confirmation with
    member this.ToEntity() =
        let result = ConfirmationEntity()

        match this with
        | Disabled -> result.Type <- DISABLED
        | ForAppointment appointmentId ->
            result.Type <- APPOINTMENT
            result.AppointmentId <- Some appointmentId.ValueStr
        | FirstAvailable -> result.Type <- FIRST_AVAILABLE
        | LastAvailable -> result.Type <- LAST_AVAILABLE
        | FirstAvailableInPeriod(min, max) ->
            result.Type <- FIRS_AVAILABLE_IN_PERIOD
            result.DateStart <- Nullable min
            result.DateEnd <- Nullable max

        result
