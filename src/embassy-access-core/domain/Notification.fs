[<AutoOpen>]
module EA.Core.Domain.Notification

open Infrastructure.Domain

type Notification =
    | Successfully of string
    | Unsuccessfully of Error'
    | HasAppointments of Set<Appointment>
    | HasConfirmations of Set<Confirmation>

    static member tryCreateFail error filter =
        match error |> filter with
        | true -> Unsuccessfully error |> Some
        | false -> None

    static member tryCreate errorFilter =
        fun request ->
            match request.ProcessState with
            | Completed msg ->
                match request.Appointments.IsEmpty with
                | true -> Successfully msg |> Some
                | false ->
                    match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                    | [] -> request.Appointments |> HasAppointments |> Some
                    | confirmations -> confirmations |> Set.ofList |> HasConfirmations |> Some
            | Failed error -> Notification.tryCreateFail error errorFilter
            | _ -> None
