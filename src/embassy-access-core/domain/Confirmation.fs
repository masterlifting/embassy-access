[<AutoOpen>]
module EA.Core.Domain.Confirmation

open System

type Confirmation =
    | Disabled
    | ForAppointment of AppointmentId
    | FirstAvailable
    | LastAvailable
    | DateTimeRange of DateTime * DateTime
