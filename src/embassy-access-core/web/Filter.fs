[<RequireQualifiedAccess>]
module EmbassyAccess.Web.Filter

open System
open EmbassyAccess

type Request =
    | NotifyAppointments of Domain.Request
    | NotifyConfirmations of Domain.Request
