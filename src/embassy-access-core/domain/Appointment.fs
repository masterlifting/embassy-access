[<AutoOpen>]
module EA.Core.Domain.Appointment

open System
open Infrastructure.Domain
open Infrastructure.Prelude

type AppointmentId =
    | AppointmentId of UUID16

    member this.Value =
        match this with
        | AppointmentId id -> id

    member this.ValueStr = this.Value.Value

    static member parse value =
        match value with
        | AP.IsUUID16 id -> AppointmentId id |> Ok
        | _ -> $"AppointmentId '{value}' is not supported." |> NotSupported |> Error

    static member createNew() = AppointmentId <| UUID16.createNew ()

type Appointment = {
    Id: AppointmentId
    Value: string
    Date: DateOnly
    Time: TimeOnly
    Description: string
} with
    static member print (appointment: Appointment) =
        let date = appointment.Date.ToString("yyyy-MM-dd")
        let time = appointment.Time.ToString("HH:mm")
        $"Available appointment '%s{appointment.Value}' on '%s{date} %s{time}'"
        
