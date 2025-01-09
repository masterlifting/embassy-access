[<AutoOpen>]
module EA.Embassies.Russian.Kdmid.Domain.KdmidRequest

open System
open EA.Core.Domain

type KdmidRequest =
    { Uri: Uri
      Embassy: EmbassyNode
      Service: ServiceNode
      Confirmation: ConfirmationState }

    member this.ToNewCoreRequest() =
        { Id = RequestId.New
          Service =
            { Id = this.Service.Id
              Name = this.Service.Name
              Payload = this.Uri |> string
              Description = this.Service.Description
              Embassy = this.Embassy }
          Attempt = (DateTime.UtcNow, 0)
          ProcessState = Created
          ConfirmationState = this.Confirmation
          Appointments = Set.empty
          Modified = DateTime.UtcNow }
