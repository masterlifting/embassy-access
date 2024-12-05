[<AutoOpen>]
module EA.Core.Domain.ConfirmationOption

open System

type ConfirmationOption =
    | FirstAvailable
    | LastAvailable
    | DateTimeRange of DateTime * DateTime
