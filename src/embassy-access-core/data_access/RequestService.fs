module EA.Core.DataAccess.RequestService

open System
open Infrastructure.Domain
open EA.Core.Domain

type RequestServiceEntity() =
    member val Payload = String.Empty with get, set
    member val ServiceId = String.Empty with get, set
    member val ServiceName = String.Empty with get, set
    member val ServiceDescription: string option = None with get, set
    member val EmbassyId = String.Empty with get, set
    member val EmbassyName = String.Empty with get, set
    member val EmbassyDescription: string option = None with get, set
    member val EmbassyTimeZone: float = 0. with get, set

    member this.ToDomain() = {
        Id = this.ServiceId |> Graph.NodeIdValue
        Name = this.ServiceName
        Payload = this.Payload
        Description = this.ServiceDescription
        Embassy = {
            Id = this.EmbassyId |> Graph.NodeIdValue
            Name = this.EmbassyName
            Description = this.EmbassyDescription
            TimeZone = this.EmbassyTimeZone
        }
    }

type internal RequestService with
    member this.ToEntity() =
        RequestServiceEntity(
            Payload = this.Payload,
            ServiceId = this.Id.Value,
            ServiceName = this.Name,
            ServiceDescription = this.Description,
            EmbassyId = this.Embassy.Id.Value,
            EmbassyName = this.Embassy.Name,
            EmbassyDescription = this.Embassy.Description,
            EmbassyTimeZone = this.Embassy.TimeZone
        )
