module EA.Italian.Services.DataAccess.Prenotami

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.SerDe
open EA.Core.DataAccess.Appointment
open EA.Italian.Services.Domain.Prenotami

let private result = ResultBuilder()

[<RequireQualifiedAccess>]
module internal Credentials =
    
    type Entity() =
        member val Login = String.Empty with get, set
        member val Password = String.Empty with get, set

        member this.ToDomain() =
            result {

                let! login =
                    match this.Login with
                    | "" -> $"Login '{this.Login}' is not supported." |> NotSupported |> Error
                    | v -> v |> Ok

                let! password =
                    match this.Password with
                    | "" -> $"Password '{this.Password}' is not supported." |> NotSupported |> Error
                    | v -> v |> Ok

                return {
                    Login = login
                    Password = password
                }
            }

[<RequireQualifiedAccess>]
module internal PayloadState =
    
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
module internal Payload =
    type Entity() =
        member val State = PayloadState.Entity() with get, set
        member val Credentials = Credentials.Entity() with get, set

        member this.ToDomain() =
            result {
                let! credentials = this.Credentials.ToDomain()
                let! state = this.State.ToDomain()

                return {
                    Credentials = credentials
                    State = state
                }
            }

type Credentials with
    member internal this.ToEntity() =
     Credentials.Entity(Login = this.Login, Password = this.Password)
     
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
    member internal this.ToEntity() =
        let result = Payload.Entity()

        result.Credentials <- this.Credentials.ToEntity()
        result.State <- this.State.ToEntity()

        result

    static member serialize key (payload: Payload) =
        payload.Credentials.Password
        |> String.encrypt key
        |> Result.map (fun password ->
            {
                payload with
                    Payload.Credentials.Password = password
            })
        |> Result.map _.ToEntity()
        |> Result.bind Json.serialize

    static member deserialize key (payload: string) =
        payload
        |> Json.deserialize<Payload.Entity>
        |> Result.bind _.ToDomain()
        |> Result.bind (fun payload ->
            payload.Credentials.Password
            |> String.decrypt key
            |> Result.map (fun password -> {
                payload with
                    Payload.Credentials.Password = password
            }))
