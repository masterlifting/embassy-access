[<AutoOpen>]
module EA.Embassies.Russian.Midpass.Domain.MidpassRequest

open System
open EA.Core.Domain

type MidpassRequest =
    { Embassy: EmbassyNode
      StatementNumber: string }

    member internal this.CreateRequest serviceName =
        { Id = RequestId.New
          Service =
            { Name = serviceName
              Payload = this.StatementNumber
              Embassy = this.Embassy
              Description = None }
          Attempt = (DateTime.UtcNow, 0)
          ProcessState = Created
          ConfirmationState = Disabled
          Appointments = Set.empty
          Modified = DateTime.UtcNow }
