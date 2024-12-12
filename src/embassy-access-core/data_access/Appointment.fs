module EA.Core.DataAccess.Appointment

open System
open EA.Core.Domain
open EA.Core.DataAccess.Confirmation

type AppointmentEntity() =
    member val Id = Guid.Empty with get, set
    member val Value = String.Empty with get, set
    member val Confirmation: ConfirmationEntity option = None with get, set
    member val DateTime = DateTime.UtcNow with get, set
    member val Description = String.Empty with get, set

    member this.ToDomain() =
        { Id = this.Id |> AppointmentId
          Value = this.Value
          Confirmation = this.Confirmation |> Option.map _.ToDomain()
          Date = DateOnly.FromDateTime(this.DateTime)
          Time = TimeOnly.FromDateTime(this.DateTime)
          Description = this.Description }

type internal Appointment with
    member internal this.ToEntity() =
        let result = AppointmentEntity()
        result.Id <- this.Id.Value
        result.Value <- this.Value
        result.Confirmation <- this.Confirmation |> Option.map _.ToEntity()
        result.DateTime <- this.Date.ToDateTime(this.Time)
        result.Description <- this.Description
        result
