[<AutoOpen>]
module EA.Core.Domain.Notification

open Infrastructure

type Notification =
    | Appointments of (EmbassyGraph * Set<Appointment>)
    | Confirmations of (RequestId * EmbassyGraph * Set<Confirmation>)
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
