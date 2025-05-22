module EA.Italian.Services.DataAccess.Prenotami

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.DataAccess.Appointment
open EA.Italian.Services.Domain.Prenotami

let private result = ResultBuilder()

[<RequireQualifiedAccess>]
module Credentials =

    type Entity() =
        member val Login = String.Empty with get, set
        member val Password = String.Empty with get, set

        member this.ToDomain key =
            result {

                let! login =
                    match this.Login with
                    | "" -> $"Login '{this.Login}' is not supported." |> NotSupported |> Error
                    | l -> l |> Ok

                let! password =
                    match this.Password with
                    | "" -> $"Password '{this.Password}' is not supported." |> NotSupported |> Error
                    | p -> p |> String.decrypt key

                return { Login = login; Password = password }
            }

[<RequireQualifiedAccess>]
module PayloadState =

    [<Literal>]
    let NO_APPOINTMENTS = nameof NoAppointments

    [<Literal>]
    let HAS_APPOINTMENTS = nameof HasAppointments

    type Entity() =
        member val Type = NO_APPOINTMENTS with get, set
        member val Appointments = Array.empty<AppointmentEntity> with get, set

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
            | _ ->
                $"The '%s{this.Type}' of '{nameof PayloadState}' is not supported."
                |> NotSupported
                |> Error

[<RequireQualifiedAccess>]
module Payload =
    type Entity() =
        member val State = PayloadState.Entity() with get, set
        member val Credentials = Credentials.Entity() with get, set

type Credentials with
    member internal this.ToEntity key =
        this.Password
        |> String.encrypt key
        |> Result.map (fun password -> Credentials.Entity(Login = this.Login, Password = password))

type PayloadState with
    member internal this.ToEntity() =
        let result = PayloadState.Entity()

        match this with
        | NoAppointments -> result.Type <- PayloadState.NO_APPOINTMENTS
        | HasAppointments appointments ->
            result.Type <- PayloadState.HAS_APPOINTMENTS
            result.Appointments <- appointments |> Seq.map _.ToEntity() |> Seq.toArray

        result

type Payload with

    static member toEntity key (payload: Payload) =
        payload.Credentials.ToEntity key
        |> Result.map (fun credentials ->
            let state = payload.State.ToEntity()
            Payload.Entity(Credentials = credentials, State = state))

    static member toDomain key (payload: Payload.Entity) =
        result {
            let! credentials = payload.Credentials.ToDomain key
            let! state = payload.State.ToDomain()

            return {
                Credentials = credentials
                State = state
            }
        }

type StorageType = EA.Core.DataAccess.Request.Storage<Payload, Payload.Entity>
