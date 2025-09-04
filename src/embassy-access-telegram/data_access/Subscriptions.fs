[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Subscriptions

open System
open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Domain

type Entity() =
    member val Id = String.Empty with get, set
    member val ServiceId = String.Empty with get, set
    member val EmbassyId = String.Empty with get, set

    member this.ToDomain() =
        match RequestId.parse this.Id, Tree.NodeId.parse this.EmbassyId, Tree.NodeId.parse this.ServiceId with
        | Ok id, Ok embassyId, Ok serviceId ->
            {
                Id = id
                EmbassyId = embassyId |> EmbassyId
                ServiceId = serviceId |> ServiceId
            }
            |> Ok
        | _ -> $"Subscription '{this.Id}' is not supported." |> NotSupported |> Error
