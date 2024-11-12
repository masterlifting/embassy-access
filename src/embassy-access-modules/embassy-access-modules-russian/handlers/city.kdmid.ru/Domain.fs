module internal EA.Embassies.Russian.Kdmid.Domain

open System
open EA.Core.Domain

type Request =
    { Country: Country
      Url: Uri
      Confirmation: ConfirmationState }
