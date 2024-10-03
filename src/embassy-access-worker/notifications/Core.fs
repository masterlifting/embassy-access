module EmbassyAccess.Worker.Notifications.Core

open EmbassyAccess.Domain

module Create =

    let appointments request =
        match request.State with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false -> Some(request.Embassy, request.Appointments)
        | _ -> None

    let confirmations request =
        match request.State with
        | Completed _ ->
            match request.Appointments.IsEmpty with
            | true -> None
            | false ->
                match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                | [] -> None
                | confirmations -> Some(request.Id, request.Embassy, confirmations)
        | _ -> None
