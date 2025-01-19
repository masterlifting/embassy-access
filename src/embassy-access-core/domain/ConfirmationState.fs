[<AutoOpen>]
module EA.Core.Domain.ConfirmationState

type ConfirmationState =
    | Disabled
    | Manual of AppointmentId
    | Auto of ConfirmationOption
