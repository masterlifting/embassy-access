[<AutoOpen>]
module EA.Core.Domain.Confirmation

open System

type Confirmation =
    | Disabled
    | FirstAvailable
    | FirstAvailableInPeriod of DateTime * DateTime
    | LastAvailable
    | ForAppointment of AppointmentId
