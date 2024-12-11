[<AutoOpen>]
module EA.Core.Domain.Appointment

open System
open Infrastructure.Domain
open Infrastructure.Prelude

type AppointmentId =
    | AppointmentId of Guid

    member this.Value =
        match this with
        | AppointmentId id -> id

    static member create value =
        match value with
        | AP.IsGuid id -> AppointmentId id |> Ok
        | _ -> $"AppointmentId value: {value}" |> NotSupported |> Error

    static member New = AppointmentId <| Guid.NewGuid()

type Appointment =
    { Id: AppointmentId
      Value: string
      Date: DateOnly
      Time: TimeOnly
      Confirmation: Confirmation option
      Description: string }
