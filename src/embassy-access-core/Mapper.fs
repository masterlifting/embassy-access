[<RequireQualifiedAccess>]
module EA.Core.Mapper

open System
open Infrastructure
open EA.Core.Domain

module Embassy =
    let rec toInternal (graph: External.Graph) =

        let embassy: Embassy =
            { Id = graph.Id |> Graph.NodeIdValue
              Name = graph.Name }

        let children =
            match graph.Children with
            | null -> Array.empty
            | children -> children |> Seq.map toInternal |> Seq.toArray

        Graph.Node(embassy, children |> Array.toList)

module Confirmation =
    let toExternal (confirmation: Confirmation) =
        let result = External.Confirmation()

        result.Description <- confirmation.Description

        result

    let toInternal (confirmation: External.Confirmation) =
        { Description = confirmation.Description }

module Appointment =
    let toExternal (appointment: Appointment) =
        let result = External.Appointment()

        result.Id <- appointment.Id.Value
        result.Value <- appointment.Value
        result.Confirmation <- appointment.Confirmation |> Option.map Confirmation.toExternal
        result.Description <- appointment.Description
        result.DateTime <- appointment.Date.ToDateTime(appointment.Time)

        result

    let toInternal (appointment: External.Appointment) =
        { Id = appointment.Id |> AppointmentId
          Value = appointment.Value
          Date = DateOnly.FromDateTime(appointment.DateTime)
          Time = TimeOnly.FromDateTime(appointment.DateTime)
          Confirmation = appointment.Confirmation |> Option.map Confirmation.toInternal
          Description = appointment.Description }

module ConfirmationOption =
    [<Literal>]
    let FIRST_AVAILABLE = nameof FirstAvailable

    [<Literal>]
    let LAST_AVAILABLE = nameof LastAvailable

    [<Literal>]
    let DATE_TIME_RANGE = nameof DateTimeRange

    let toExternal option =
        let result = External.ConfirmationOption()

        match option with
        | FirstAvailable -> result.Type <- FIRST_AVAILABLE
        | LastAvailable -> result.Type <- LAST_AVAILABLE
        | DateTimeRange(min, max) ->
            result.Type <- DATE_TIME_RANGE
            result.DateStart <- Nullable min
            result.DateEnd <- Nullable max

        result

    let toInternal (option: External.ConfirmationOption) =
        match option.Type with
        | FIRST_AVAILABLE -> FirstAvailable |> Ok
        | LAST_AVAILABLE -> LastAvailable |> Ok
        | DATE_TIME_RANGE ->
            match option.DateStart |> Option.ofNullable, option.DateEnd |> Option.ofNullable with
            | Some min, Some max -> DateTimeRange(min, max) |> Ok
            | _ -> "DateStart or DateEnd" |> NotFound |> Error
        | _ -> $"ConfirmationOption %s{option.Type}" |> NotSupported |> Error

module ConfirmationState =

    [<Literal>]
    let DISABLED = nameof Disabled

    [<Literal>]
    let MANUAL = nameof Manual

    [<Literal>]
    let AUTO = nameof Auto

    let toExternal (state: ConfirmationState) =
        let result = External.ConfirmationState()

        match state with
        | Disabled -> result.Type <- DISABLED
        | Manual appointmentId ->
            result.Type <- MANUAL
            result.AppointmentId <- Some appointmentId.Value
        | Auto option ->
            result.Type <- AUTO
            result.ConfirmationOption <- Some option |> Option.map ConfirmationOption.toExternal

        result

    let toInternal (state: External.ConfirmationState) =
        match state.Type with
        | DISABLED -> Disabled |> Ok
        | MANUAL ->
            match state.AppointmentId with
            | Some id -> id |> AppointmentId |> Manual |> Ok
            | None -> nameof AppointmentId |> NotFound |> Error
        | AUTO ->
            match state.ConfirmationOption with
            | Some option -> option |> ConfirmationOption.toInternal |> Result.map Auto
            | None -> nameof ConfirmationOption |> NotFound |> Error
        | _ -> $"ConfirmationType %s{state.Type}" |> NotSupported |> Error

module ProcessState =
    [<Literal>]
    let CREATED = nameof Created

    [<Literal>]
    let IN_PROCESS = nameof InProcess

    [<Literal>]
    let COMPLETED = nameof Completed

    [<Literal>]
    let FAILED = nameof Failed

    let toExternal state =
        let result = External.ProcessState()

        match state with
        | Created -> result.Type <- CREATED
        | InProcess -> result.Type <- IN_PROCESS
        | Completed msg ->
            result.Type <- COMPLETED
            result.Message <- Some msg
        | Failed error ->
            result.Type <- FAILED
            result.Error <- error |> Mapper.Error.toExternal |> Some

        result

    let toInternal (state: External.ProcessState) =
        match state.Type with
        | CREATED -> Created |> Ok
        | IN_PROCESS -> InProcess |> Ok
        | COMPLETED ->
            let msg =
                match state.Message with
                | Some(AP.IsString value) -> value
                | _ -> "Message not found."

            Completed msg |> Ok
        | FAILED ->
            match state.Error with
            | Some error -> error |> Mapper.Error.toInternal |> Result.map Failed
            | None -> "Failed state without error" |> NotSupported |> Error
        | _ -> $"Request state %s{state.Type}" |> NotSupported |> Error

module Service =
    let toExternal service =
        let result = External.Service()

        result.Name <- service.Name
        result.Payload <- service.Payload
        result.Embassy <- service.Embassy
        result.Description <- service.Description

        result

    let toInternal (service: External.Service) =
        { Name = service.Name
          Payload = service.Payload
          Embassy = service.Embassy
          Description = service.Description }

module Request =
    let toExternal request =
        let result = External.Request()

        result.Id <- request.Id.Value
        result.Service <- request.Service |> Service.toExternal
        result.Attempt <- request.Attempt |> snd
        result.AttemptModified <- request.Attempt |> fst
        result.ProcessState <- request.ProcessState |> ProcessState.toExternal
        result.ConfirmationState <- request.ConfirmationState |> ConfirmationState.toExternal
        result.Appointments <- request.Appointments |> Seq.map Appointment.toExternal |> Seq.toArray
        result.Modified <- request.Modified

        result

    let toInternal (request: External.Request) =
        let requestResult = ResultBuilder()

        requestResult {

            let service = request.Service |> Service.toInternal
            let! processState = request.ProcessState |> ProcessState.toInternal
            let! confirmationState = request.ConfirmationState |> ConfirmationState.toInternal

            let appointments =
                request.Appointments |> Seq.map Appointment.toInternal |> Set.ofSeq

            return
                { Id = request.Id |> RequestId
                  Service = service
                  Attempt = request.AttemptModified, request.Attempt
                  ProcessState = processState
                  ConfirmationState = confirmationState
                  Appointments = appointments
                  Modified = request.Modified }
        }
