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
        match RequestId.create this.Id with
        | Ok id ->
            {
                Id = id
                EmbassyId = this.EmbassyId |> EmbassyId
                ServiceId = this.ServiceId |> ServiceId
            }
            |> Ok
        | _ -> $"Subscription '{this.Id}' is not supported." |> NotSupported |> Error
