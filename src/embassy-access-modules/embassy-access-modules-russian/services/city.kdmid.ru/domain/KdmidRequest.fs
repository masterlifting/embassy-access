﻿[<AutoOpen>]
module EA.Embassies.Russian.Kdmid.Domain.KdmidRequest

open System
open EA.Core.Domain

type KdmidRequest = {
    Uri: Uri
    Embassy: EmbassyNode
    Service: ServiceNode
    SubscriptionState: SubscriptionState
    ConfirmationState: ConfirmationState
} with

    member this.ToRequest() = {
        Id = RequestId.createNew ()
        Service = {
            Id = this.Service.Id
            Name = this.Service.Name
            Payload = this.Uri |> string
            Description = this.Service.Description
            Embassy = this.Embassy
        }
        Attempt = (DateTime.UtcNow, 0)
        ProcessState = Ready
        SubscriptionState = this.SubscriptionState
        ConfirmationState = this.ConfirmationState
        Appointments = Set.empty
        Modified = DateTime.UtcNow
    }
