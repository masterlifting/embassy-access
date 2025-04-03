[<AutoOpen>]
module EA.Core.Domain.ConfirmationState

open System

type ConfirmationState =
    | Disabled
    | Appointment of AppointmentId
    | FirstAvailable
    | LastAvailable
    | DateTimeRange of DateTime * DateTime
