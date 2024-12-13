[<AutoOpen>]
module EA.Embassies.Russian.Kdmid.Domain.KdmidRequest

open System
open EA.Core.Domain

type KdmidRequest =
    { Uri: Uri
      Embassy: EmbassyNode
      TimeZone: float
      Confirmation: ConfirmationState }

    member this.CreateRequest serviceName =
        { Id = RequestId.New
          Service =
            { Name = serviceName
              Payload = this.Uri.ToString()
              Embassy = this.Embassy
              Description = None }
          Attempt = (DateTime.UtcNow, 0)
          ProcessState = Created
          ConfirmationState = this.Confirmation
          Appointments = Set.empty
          Modified = DateTime.UtcNow }


