module EA.Embassies.Russian.Midpass.Domain

open System
open EA.Core.Domain

type ServiceRequest =
    { Embassy: EmbassyGraph
      StatementNumber: string }

    member internal this.Create serviceName =
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

type Service = { Request: ServiceRequest }
