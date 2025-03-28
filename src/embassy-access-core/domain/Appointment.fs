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
        | _ -> $"AppointmentId '{value}'" |> NotSupported |> Error

    static member createNew() = AppointmentId <| UUID16.createNew ()

type Appointment =
    { Id: AppointmentId
      Value: string
      Date: DateOnly
      Time: TimeOnly
      Confirmation: Confirmation option
      Description: string }
