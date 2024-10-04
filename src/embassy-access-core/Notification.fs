module EmbassyAccess.Worker.Notification

open EmbassyAccess.Domain

module Create =

    let searchAppointments request =
        match request.State with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false -> Some <| SearchAppointments(request.Id, request.Embassy, request.Appointments)
        | _ -> None

    let makeConfirmations request =
        match request.State with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false ->
                match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                | [] -> None
                | confirmations -> Some <| MakeConfirmations(request.Id, request.Embassy, confirmations |> set)
        | _ -> None
