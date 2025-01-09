[<AutoOpen>]
module EA.Embassies.Russian.Midpass.Domain.MidpassRequest

open System
open EA.Core.Domain

type MidpassRequest =
    { StatementNumber: string
      Embassy: EmbassyNode
      Service: ServiceNode }

    member internal this.ToNewCoreRequest() =
        { Id = RequestId.New
          Service =
            { Id = this.Service.Id
              Name = this.Service.Name
              Description = this.Service.Description
              Payload = this.StatementNumber
              Embassy = this.Embassy }
          Attempt = (DateTime.UtcNow, 0)
          ProcessState = Created
          ConfirmationState = Disabled
          Appointments = Set.empty
          Modified = DateTime.UtcNow }
