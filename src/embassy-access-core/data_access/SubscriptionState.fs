module EA.Core.DataAccess.SubscriptionState

open System
open Infrastructure.Domain
open EA.Core.Domain

[<Literal>]
let private MANUAL = nameof SubscriptionState.Manual

[<Literal>]
let private AUTO = nameof SubscriptionState.Auto

type SubscriptionStateEntity() =

    member val Type = String.Empty with get, set

    member this.ToDomain() =
        match this.Type with
        | MANUAL -> SubscriptionState.Manual |> Ok
        | AUTO -> SubscriptionState.Auto |> Ok
        | _ ->
            $"The '%s{this.Type}' of '{nameof SubscriptionStateEntity}'"
            |> NotSupported
            |> Error

type internal SubscriptionState with
    member internal this.ToEntity() =
        let result = SubscriptionStateEntity()

        match this with
        | SubscriptionState.Manual -> result.Type <- MANUAL
        | SubscriptionState.Auto -> result.Type <- AUTO

        result
