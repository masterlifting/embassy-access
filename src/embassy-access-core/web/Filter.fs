[<RequireQualifiedAccess>]
module EmbassyAccess.Web.Filter

open EmbassyAccess

type Request =
    | ShareAppointments of Domain.Embassy * Domain.Appointment seq
    | SendAppointments of Domain.RequestId * Domain.Appointment seq
    | SendConfirmations of Domain.RequestId * Domain.Confirmation seq
