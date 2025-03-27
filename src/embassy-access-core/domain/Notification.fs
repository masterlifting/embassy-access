[<AutoOpen>]
module EA.Core.Domain.Notification

open Infrastructure.Domain

type Notification =
    | Empty of Request * string
    | Unsuccessfully of Request * Error'
    | HasAppointments of Request * Set<Appointment>
    | HasConfirmations of Request * Set<Confirmation>

    static member tryCreateFail error filter =
        fun request ->
            match error |> filter with
            | true -> Unsuccessfully(request, error) |> Some
            | false -> None

    static member tryCreate errorFilter =
        fun request ->
            match request.ProcessState with
            | Completed msg ->
                match request.Appointments.IsEmpty with
                | true -> Empty(request, msg) |> Some
                | false ->
                    match request.Appointments |> Seq.choose _.Confirmation |> List.ofSeq with
                    | [] -> (request, request.Appointments) |> HasAppointments |> Some
                    | confirmations -> (request, confirmations |> Set.ofList) |> HasConfirmations |> Some
            | Failed error -> request |> Notification.tryCreateFail error errorFilter
            | _ -> None
