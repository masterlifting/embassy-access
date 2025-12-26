module EA.Russian.DataAccess.Kdmid

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess.Appointment
open EA.Core.DataAccess.Confirmation
open EA.Russian.Domain.Kdmid

let private result = ResultBuilder()

[<RequireQualifiedAccess>]
module Credentials =

    type Entity() =
        member val Id = 0 with get, set
        member val Cd = String.Empty with get, set
        member val Ems: string option = None with get, set
        member val Subdomain = String.Empty with get, set

        member this.ToDomain() =
            result {
                let! subdomain =
                    match this.Subdomain with
                    | "" -> $"Subdomain '{this.Subdomain}' is not supported." |> NotSupported |> Error
                    | subdomain -> subdomain |> Ok

                let! id =
                    match this.Id with
                    | 0 -> $"Id '{this.Id}' is not supported." |> NotSupported |> Error
                    | id -> id |> Ok

                let! cd =
                    match this.Cd with
                    | "" -> $"Cd '{this.Cd}' is not supported." |> NotSupported |> Error
                    | cd -> cd |> Ok

                let! ems =
                    match this.Ems with
                    | Some ems when ems = "" -> $"Ems '{this.Ems}' is not supported." |> NotSupported |> Error
                    | Some ems -> ems |> Some |> Ok
                    | None -> None |> Ok

                return {
                    Subdomain = subdomain
                    Id = id
                    Cd = cd
                    Ems = ems
                }
            }

[<RequireQualifiedAccess>]
module PayloadState =

    [<Literal>]
    let NO_APPOINTMENTS = nameof NoAppointments

    [<Literal>]
    let HAS_APPOINTMENTS = nameof HasAppointments

    [<Literal>]
    let HAS_CONFIRMATION = nameof HasConfirmation

    type Entity() =
        member val Type = NO_APPOINTMENTS with get, set
        member val Appointments = Array.empty<AppointmentEntity> with get, set
        member val ConfirmationMessage = String.Empty with get, set
        member val ConfirmedAppointment: AppointmentEntity | null = null with get, set

        member this.ToDomain() =
            match this.Type with
            | NO_APPOINTMENTS -> NoAppointments |> Ok
            | HAS_APPOINTMENTS ->
                this.Appointments
                |> Array.map _.ToDomain()
                |> Result.choose
                |> Result.bind (fun appointments ->
                    match appointments.IsEmpty with
                    | true -> $"{nameof this.Appointments} is empty." |> NotFound |> Error
                    | false -> appointments |> Set.ofList |> HasAppointments |> Ok)
            | HAS_CONFIRMATION ->
                match this.ConfirmedAppointment with
                | null ->
                    $"'{nameof this.ConfirmedAppointment}' of '{nameof PayloadState}' is not supported."
                    |> NotFound
                    |> Error
                | appointment ->
                    appointment.ToDomain()
                    |> Result.map (fun appointment -> HasConfirmation(this.ConfirmationMessage, appointment))
            | _ ->
                $"The '%s{this.Type}' of '{nameof PayloadState}' is not supported."
                |> NotSupported
                |> Error

[<RequireQualifiedAccess>]
module Payload =
    type Entity() =
        member val State = PayloadState.Entity() with get, set
        member val Credentials = Credentials.Entity() with get, set
        member val Confirmation = Disabled.ToEntity() with get, set

type Credentials with
    member internal this.ToEntity() =
        Credentials.Entity(Id = this.Id, Cd = this.Cd, Ems = this.Ems, Subdomain = this.Subdomain)

type PayloadState with
    member internal this.ToEntity() =
        let result = PayloadState.Entity()

        match this with
        | NoAppointments -> result.Type <- PayloadState.NO_APPOINTMENTS
        | HasAppointments appointments ->
            result.Type <- PayloadState.HAS_APPOINTMENTS
            result.Appointments <- appointments |> Seq.map _.ToEntity() |> Seq.toArray
        | HasConfirmation(message, appointment) ->
            result.Type <- PayloadState.HAS_CONFIRMATION
            result.ConfirmationMessage <- message
            result.ConfirmedAppointment <- appointment.ToEntity()

        result

type Payload with

    static member toEntity(payload: Payload) =
        let result = Payload.Entity()

        result.Credentials <- payload.Credentials.ToEntity()
        result.Confirmation <- payload.Confirmation.ToEntity()
        result.State <- payload.State.ToEntity()

        result |> Ok

    static member toDomain(payload: Payload.Entity) =
        result {
            let! credentials = payload.Credentials.ToDomain()
            let! confirmation = payload.Confirmation.ToDomain()
            let! state = payload.State.ToDomain()

            return {
                Credentials = credentials
                Confirmation = confirmation
                State = state
            }
        }

type StorageType = EA.Core.DataAccess.Request.Storage<Payload, Payload.Entity>
