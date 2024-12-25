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

    member this.ToDomain() =
        { Id = this.ServiceId |> Graph.NodeIdValue
          Name = this.ServiceName
          Payload = this.Payload
          Description = this.ServiceDescription
          Embassy =
            { Id = this.EmbassyId |> Graph.NodeIdValue
              Name = this.EmbassyName
              Description = this.EmbassyDescription } }

type internal RequestService with
    member internal this.ToEntity() =
        let result = RequestServiceEntity()
        result.Payload <- this.Payload

        result.ServiceId <- this.Id.Value
        result.ServiceName <- this.Name
        result.ServiceDescription <- this.Description

        result.EmbassyId <- this.Embassy.Id.Value
        result.EmbassyName <- this.Embassy.Name
        result.EmbassyDescription <- this.Embassy.Description
        result
