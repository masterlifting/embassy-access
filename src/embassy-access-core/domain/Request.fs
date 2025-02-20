﻿[<AutoOpen>]
module EA.Core.Domain.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude

type RequestId =
    | RequestId of Guid

    member this.Value =
        match this with
        | RequestId id -> id

    member this.ValueStr = this.Value |> string

    static member create value =
        match value with
        | AP.IsGuid id -> RequestId id |> Ok
        | _ -> $"RequestId value: {value}" |> NotSupported |> Error

    static member New = RequestId <| Guid.NewGuid()

type Request =
    { Id: RequestId
      Service: RequestService
      Attempt: DateTime * int
      ProcessState: ProcessState
      SubscriptionState: SubscriptionState
      ConfirmationState: ConfirmationState
      Appointments: Set<Appointment>
      Modified: DateTime }
