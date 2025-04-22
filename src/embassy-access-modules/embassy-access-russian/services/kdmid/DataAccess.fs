module EA.Russian.Services.DataAccess.Kdmid

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open EA.Core.Domain
open EA.Core.DataAccess.Appointment
open EA.Core.DataAccess.Confirmation
open EA.Russian.Services.Domain.Kdmid

let private result = ResultBuilder()

type CredentialsEntity() =
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

type Credentials with
    member this.ToEntity() =
        CredentialsEntity(Id = this.Id, Cd = this.Cd, Ems = this.Ems, Subdomain = this.Subdomain)

[<Literal>]
let private NO_APPOINTMENTS = nameof NoAppointments

[<Literal>]
let private HAS_APPOINTMENTS = nameof HasAppointments

[<Literal>]
let private HAS_CONFIRMATION = nameof HasConfirmation

type PayloadStateEntity() =
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
                $"'{nameof this.ConfirmedAppointment}' of '{nameof PayloadStateEntity}' is not supported."
                |> NotFound
                |> Error
            | appointment ->
                appointment.ToDomain()
                |> Result.map (fun appointment -> HasConfirmation(this.ConfirmationMessage, appointment))
        | _ ->
            $"The '%s{this.Type}' of '{nameof PayloadStateEntity}' is not supported."
            |> NotSupported
            |> Error

type PayloadState with
    member this.ToEntity() =
        let result = PayloadStateEntity()

        match this with
        | NoAppointments -> result.Type <- NO_APPOINTMENTS
        | HasAppointments appointments ->
            result.Type <- HAS_APPOINTMENTS
            result.Appointments <- appointments |> Seq.map _.ToEntity() |> Seq.toArray
        | HasConfirmation(message, appointment) ->
            result.Type <- HAS_CONFIRMATION
            result.ConfirmationMessage <- message
            result.ConfirmedAppointment <- appointment.ToEntity()

        result

type PayloadEntity() =
    member val Credentials = CredentialsEntity() with get, set
    member val Confirmation = Disabled.ToEntity() with get, set
    member val State = PayloadStateEntity() with get, set

    member this.ToDomain() =
        result {
            let! credentials = this.Credentials.ToDomain()
            let! confirmation = this.Confirmation.ToDomain()
            let! state = this.State.ToDomain()

            return {
                Credentials = credentials
                Confirmation = confirmation
                State = state
            }
        }

type Payload with
    member this.ToEntity() =
        let result = PayloadEntity()

        result.Credentials <- this.Credentials.ToEntity()
        result.Confirmation <- this.Confirmation.ToEntity()
        result.State <- this.State.ToEntity()

        result

    static member serialize(payload: Payload) = payload.ToEntity() |> Json.serialize

    static member deserialize(payload: string) =
        payload |> Json.deserialize<PayloadEntity> |> Result.bind _.ToDomain()
