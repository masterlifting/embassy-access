module EA.Notification

open EA.Domain

module Create =

    let appointments request =
        match request.ProcessState with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false -> Some <| Appointments(request.Embassy, request.Appointments)
        | _ -> None

    let confirmations request =
        match request.ProcessState with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false ->
                match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                | [] -> None
                | confirmations -> Some <| Confirmations(request.Id, request.Embassy, confirmations |> set)
        | _ -> None
