[<AutoOpen>]
module EA.Core.Domain.Notification

open Infrastructure.Domain

type Notification =
    | Appointments of (EmbassyNode * Set<Appointment>)
    | Confirmations of (RequestId * EmbassyNode * Set<Confirmation>)
    | Fail of (RequestId * Error')

    static member tryCreateFail requestId allow error =
        match error |> allow with
        | true -> Fail(requestId, error) |> Some
        | false -> None

    static member tryCreate allowError request =
        match request.ProcessState with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false ->
                match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                | [] -> Appointments(request.Service.Embassy, request.Appointments) |> Some
                | confirmations -> Confirmations(request.Id, request.Service.Embassy, confirmations |> set) |> Some
        | Failed error -> error |> Notification.tryCreateFail request.Id allowError
        | _ -> None
        
    member this.Message =
        match this with
        | Appointments(embassy, appointments) -> $"Appointments for {embassy.Name}: {appointments.Count}"
        | Confirmations(requestId, embassy, confirmations) -> $"Confirmations for {embassy.Name} request {requestId}: {confirmations.Count}"
        | Fail(requestId, error) -> $"Failed request {requestId}: {error.Message}"
