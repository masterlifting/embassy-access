[<AutoOpen>]
module EA.Core.Domain.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude

type RequestId =
    | RequestId of UUID16

    member this.Value =
        match this with
        | RequestId id -> id

    member this.ValueStr = this.Value.Value

    static member parse value =
        match value with
        | AP.IsUUID16 id -> RequestId id |> Ok
        | _ -> $"RequestId '{value}'" |> NotSupported |> Error

    static member createNew() = RequestId <| UUID16.createNew ()

type Request =
    { Id: RequestId
      Service: RequestService
      Attempt: DateTime * int
      ProcessState: ProcessState
      SubscriptionState: SubscriptionState
      ConfirmationState: ConfirmationState
      Appointments: Set<Appointment>
      Modified: DateTime }
