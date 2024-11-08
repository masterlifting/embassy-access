[<RequireQualifiedAccess>]
module EA.Persistence.Command

open System
open EA.Core.Domain

module Definitions =
    module Request =

        type PassportsGroup =
            { Name: string option
              Embassy: Embassy
              Payload: string
              ConfirmationState: ConfirmationState
              Validation: Validation option }

            member this.createRequest() =
                { Id = RequestId.New
                  Name = this.Name |> Option.defaultValue this.Payload
                  Payload = this.Payload
                  Embassy = this.Embassy
                  ProcessState = Created
                  Attempt = (DateTime.UtcNow, 0)
                  ConfirmationState = this.ConfirmationState
                  Appointments = Set.empty
                  Description = None
                  GroupBy = Some "Passports"
                  Modified = DateTime.UtcNow }

        type OthersGroup =
            { Name: string option
              Embassy: Embassy
              Payload: string
              ConfirmationState: ConfirmationState
              Validation: Validation option }

            member this.createRequest() =
                { Id = RequestId.New
                  Name = this.Name |> Option.defaultValue this.Payload
                  Payload = this.Payload
                  Embassy = this.Embassy
                  ProcessState = Created
                  Attempt = (DateTime.UtcNow, 0)
                  ConfirmationState = this.ConfirmationState
                  Appointments = Set.empty
                  Description = None
                  GroupBy = Some "Others"
                  Modified = DateTime.UtcNow }

        type PassportResultGroup =
            { Name: string option
              Embassy: Embassy
              Payload: string
              Validation: Validation option }

            member this.createRequest() =
                { Id = RequestId.New
                  Name = this.Name |> Option.defaultValue this.Payload
                  Payload = this.Payload
                  Embassy = this.Embassy
                  ProcessState = Created
                  Attempt = (DateTime.UtcNow, 0)
                  ConfirmationState = Disabled
                  Appointments = Set.empty
                  Description = None
                  GroupBy = Some "Passports"
                  Modified = DateTime.UtcNow }


        type Create =
            | PassportsGroup of PassportsGroup
            | OthersGroup of OthersGroup
            | PassportResultGroup of PassportResultGroup

        type CreateOrUpdate =
            | PassportsGroup of PassportsGroup
            | OthersGroup of OthersGroup
            | PassportResultGroup of PassportResultGroup

        type Update = Request of Request
        type Delete = RequestId of RequestId

module Request =
    type Operation =
        | Create of Definitions.Request.Create
        | CreateOrUpdate of Definitions.Request.CreateOrUpdate
        | Update of Definitions.Request.Update
        | Delete of Definitions.Request.Delete
