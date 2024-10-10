[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Command

open System
open EmbassyAccess.Domain

module Options =
    module Request =

        type PassportsGroup =
            { Embassy: Embassy
              Payload: string
              ConfirmationState: ConfirmationState
              Validation: Validation option }

            member this.createRequest() =
                { Id = RequestId.New
                  Payload = this.Payload
                  Embassy = this.Embassy
                  ProcessState = Created
                  Attempt = 0
                  ConfirmationState = this.ConfirmationState
                  Appointments = Set.empty
                  Description = None
                  GroupBy = Some "Passports"
                  Modified = DateTime.UtcNow }

        type Create = PassportsGroup of PassportsGroup
        type Update = Request of Request
        type Delete = RequestId of RequestId

module Request =
    type Operation =
        | Create of Options.Request.Create
        | Update of Options.Request.Update
        | Delete of Options.Request.Delete
