module EA.Core.DataAccess.RequestService

open System
open Infrastructure.Domain
open EA.Core.Domain

type RequestServiceEntity() =
    member val Name = String.Empty with get, set
    member val Payload = String.Empty with get, set
    member val EmbassyId = String.Empty with get, set
    member val EmbassyName = String.Empty with get, set
    member val Description: string option = None with get, set

    member this.ToDomain() =
        { Name = this.Name
          Payload = this.Payload
          Embassy =
            { Id = this.EmbassyId |> Graph.NodeIdValue
              Name = this.EmbassyName
              Description = None }
          Description = this.Description }

type internal RequestService with
    member internal this.ToEntity() =
        let result = RequestServiceEntity()
        result.Name <- this.Name
        result.Payload <- this.Payload
        result.EmbassyId <- this.Embassy.Id.Value
        result.EmbassyName <- this.Embassy.Name
        result.Description <- this.Description
        result
