[<AutoOpen>]
module EA.Embassies.Russian.Kdmid.Domain.Order

open EA.Core.Domain

type StartOrder =
    { Request: Request
      TimeZone: float }

    static member create timeZone request =
        { Request = request
          TimeZone = timeZone }

type PickOrder =
    { StartOrders: StartOrder list
      notify: Notification -> Async<unit> }
