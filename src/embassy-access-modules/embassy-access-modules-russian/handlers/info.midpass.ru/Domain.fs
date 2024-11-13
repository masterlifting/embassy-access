module EA.Embassies.Russian.Midpass.Domain

open System
open EA.Core.Domain

type Request =
    { Country: Country
      StatementNumber: string }

    member internal this.Create name : EA.Core.Domain.Request =
        { Id = RequestId.New
          Service =
            { Name = name
              Payload = this.StatementNumber
              Embassy = Russian this.Country
              Description = None }
          Attempt = (DateTime.UtcNow, 0)
          ProcessState = Created
          ConfirmationState = Disabled
          Appointments = Set.empty
          Modified = DateTime.UtcNow }
