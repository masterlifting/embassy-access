module EA.Core.DataAccess.Confirmation

open System
open EA.Core.Domain

type ConfirmationEntity() =
    member val Description = String.Empty with get, set

    member this.ToDomain() = { Description = this.Description }

type internal Confirmation with
    member this.ToEntity() =
        ConfirmationEntity(Description = this.Description)
