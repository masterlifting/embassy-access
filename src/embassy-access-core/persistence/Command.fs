[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Command

module Request =
    open EmbassyAccess.Domain

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
              Modified = System.DateTime.UtcNow }

    module Options =
        type Create = PassportsGroup of PassportsGroup
        type Update = Request of Request
        type Delete = RequestId of RequestId

    type Operation =
        | Create of Options.Create
        | Update of Options.Update
        | Delete of Options.Delete
