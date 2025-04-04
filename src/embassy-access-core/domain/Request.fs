[<AutoOpen>]
module EA.Core.Domain.Request

open System
open Infrastructure.Domain
open Infrastructure.Prelude

type RequestId =
    | RequestId of UUID16

    member this.Value =
        match this with
        | RequestId id -> id

    member this.ValueStr = this.Value.Value

    static member parse value =
        match value with
        | AP.IsUUID16 id -> RequestId id |> Ok
        | _ -> $"RequestId '{value}' is not supported." |> NotSupported |> Error

    static member createNew() = RequestId <| UUID16.createNew ()

type Request = {
    Id: RequestId
    Service: RequestService
    ProcessState: ProcessState
    ConfirmationState: ConfirmationState
    Appointments: Set<Appointment>
    Limitations: Set<Limitation>
    Retries: uint<attempts>
    Attempt: DateTime * int
    IsBackground: bool
    Modified: DateTime
}

let updateLimitations request = {
    request with
        Limitations =
            request.Limitations
            |> Seq.map (Limitation.updateState request.Modified request.Service.Embassy.TimeZone)
            |> Set.ofSeq
}
