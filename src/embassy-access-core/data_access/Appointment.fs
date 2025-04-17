module EA.Core.DataAccess.Appointment

open System
open EA.Core.Domain

type AppointmentEntity() =
    member val Id = String.Empty with get, set
    member val Value = String.Empty with get, set
    member val Confirmation: string option = None with get, set
    member val DateTime = DateTime.UtcNow with get, set
    member val Description = String.Empty with get, set

    member this.ToDomain() =
        this.Id
        |> AppointmentId.parse
        |> Result.map (fun id -> {
            Id = id
            Value = this.Value
            Confirmation = this.Confirmation
            Date = DateOnly.FromDateTime this.DateTime
            Time = TimeOnly.FromDateTime this.DateTime
            Description = this.Description
        })

type internal Appointment with
    member this.ToEntity() =
        AppointmentEntity(
            Id = this.Id.ValueStr,
            Value = this.Value,
            Confirmation = this.Confirmation,
            DateTime = this.Date.ToDateTime this.Time,
            Description = this.Description
        )
