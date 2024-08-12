module internal EmbassyAccess.Mapper

open System
open Infrastructure
open EmbassyAccess.Domain

let toConfirmation (confirmation: External.Confirmation option) : Confirmation option =
    confirmation |> Option.map (fun x -> { Description = x.Description })

let toAppointment (appointment: External.Appointment) : Appointment =
    { Value = appointment.Value
      Date = DateOnly.FromDateTime(appointment.DateTime)
      Time = TimeOnly.FromDateTime(appointment.DateTime)
      Confirmation = appointment.Confirmation |> toConfirmation
      Description =
        match appointment.Description with
        | AP.IsString x -> Some x
        | _ -> None }

let toRequest (request: External.Request) : Result<Request, Error'> =
    request.Embassy.toDU ()
    |> Result.bind (fun embassy ->
        request.State.toDU ()
        |> Result.bind (fun state ->
            { Id = RequestId request.Id
              Value = request.Value
              Attempt = request.Attempt
              State = state
              Embassy = embassy
              Appointments = request.Appointments |> Seq.map toAppointment |> Set.ofSeq
              Description =
                match request.Description with
                | AP.IsString x -> Some x
                | _ -> None
              Modified = request.Modified }
            |> Ok))

module External =

    let toConfirmation (confirmation: Confirmation option) : External.Confirmation option =
        confirmation
        |> Option.map (fun x ->
            let result = External.Confirmation()

            result.Description <- x.Description

            result)

    let toAppointment (appointment: Appointment) : External.Appointment =
        let result = External.Appointment()

        result.Value <- appointment.Value
        result.Confirmation <- appointment.Confirmation |> toConfirmation
        result.Description <- appointment.Description |> Option.defaultValue ""
        result.DateTime <- appointment.Date.ToDateTime(appointment.Time)

        result

    let toRequest (request: Request) : External.Request =
        let result = External.Request()

        result.Id <- request.Id.Value
        result.Value <- request.Value
        result.Attempt <- request.Attempt
        result.State <- request.State |> External.RequestState.fromDU
        result.Embassy <- request.Embassy |> External.Embassy.fromDU
        result.Appointments <- request.Appointments |> Seq.map toAppointment |> Seq.toArray
        result.Description <- request.Description |> Option.defaultValue ""
        result.Modified <- request.Modified

        result
