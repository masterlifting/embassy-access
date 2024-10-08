[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Command

open Infrastructure
open EmbassyAccess.Domain

type RequestValidation = (Request -> Result<unit, Error'>) option

type PassportsRequest =
    { Embassy: Embassy
      Payload: string
      ConfirmationState: ConfirmationState
      Validation: RequestValidation }

    member this.create() =
        { Id = RequestId.New
          Payload = this.Payload
          Embassy = this.Embassy
          State = Created
          Attempt = 0
          ConfirmationState = this.ConfirmationState
          Appointments = Set.empty
          Description = None
          GroupBy = Some "Passports"
          Modified = System.DateTime.UtcNow }
type CreateOptions = PassportsRequest of PassportsRequest

type Request =
    | Create of CreateOptions
    | Update of Request
    | Delete of Request
