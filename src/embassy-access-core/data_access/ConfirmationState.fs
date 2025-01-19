module EA.Core.DataAccess.ConfirmationState

open System
open Infrastructure.Domain
open EA.Core.Domain
open EA.Core.DataAccess.ConfirmationOption

[<Literal>]
let private DISABLED = nameof Disabled

[<Literal>]
let private MANUAL = nameof Manual

[<Literal>]
let private AUTO = nameof Auto

type ConfirmationStateEntity() =

    member val Type = String.Empty with get, set
    member val ConfirmationOption: ConfirmationOptionEntity option = None with get, set
    member val AppointmentId: Guid option = None with get, set

    member this.ToDomain() =
        match this.Type with
        | DISABLED -> Disabled |> Ok
        | MANUAL ->
            match this.AppointmentId with
            | Some id -> id |> AppointmentId |> Manual |> Ok
            | None -> nameof AppointmentId |> NotFound |> Error
        | AUTO ->
            match this.ConfirmationOption with
            | Some option -> option.ToDomain() |> Result.map Auto
            | None -> nameof ConfirmationOptionEntity |> NotFound |> Error
        | _ ->
            $"The %s{this.Type} of {nameof ConfirmationStateEntity}"
            |> NotSupported
            |> Error

type internal ConfirmationState with
    member internal this.ToEntity() =
        let result = ConfirmationStateEntity()

        match this with
        | Disabled -> result.Type <- DISABLED
        | Manual appointmentId ->
            result.Type <- MANUAL
            result.AppointmentId <- Some appointmentId.Value
        | Auto option ->
            result.Type <- AUTO
            result.ConfirmationOption <- Some option |> Option.map _.ToEntity()

        result
