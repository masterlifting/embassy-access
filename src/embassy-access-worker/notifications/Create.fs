[<RequireQualifiedAccess>]
module EmbassyAccess.Worker.Notifications.Create

open EmbassyAccess.Domain

let appointmentsNotification request =
    match request.State with
    | Completed _ ->
        match request.Appointments.IsEmpty with
        | true -> None
        | false -> Some(request.Embassy, request.Appointments)
    | _ -> None

let confirmationsNotification request =
    match request.State with
    | Completed _ ->
        match request.Appointments.IsEmpty with
        | true -> None
        | false ->
            match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
            | [] -> None
            | confirmations -> Some(request.Id, request.Embassy, confirmations)
    | _ -> None
