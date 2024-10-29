[<RequireQualifiedAccess>]
module EA.Mapper

open System
open Infrastructure
open EA.Domain

module City =
    [<Literal>]
    let Belgrade = nameof City.Belgrade

    [<Literal>]
    let Berlin = nameof City.Berlin

    [<Literal>]
    let Budapest = nameof City.Budapest

    [<Literal>]
    let Sarajevo = nameof City.Sarajevo

    [<Literal>]
    let Podgorica = nameof City.Podgorica

    [<Literal>]
    let Tirana = nameof City.Tirana

    [<Literal>]
    let Paris = nameof City.Paris

    [<Literal>]
    let Rome = nameof City.Rome

    [<Literal>]
    let Dublin = nameof City.Dublin

    [<Literal>]
    let Bern = nameof City.Bern

    [<Literal>]
    let Helsinki = nameof City.Helsinki

    [<Literal>]
    let Hague = nameof City.Hague

    [<Literal>]
    let Ljubljana = nameof City.Ljubljana

    let toExternal city =
        let result = External.City()

        match city with
        | City.Belgrade -> result.Name <- Belgrade
        | City.Berlin -> result.Name <- Berlin
        | City.Budapest -> result.Name <- Budapest
        | City.Sarajevo -> result.Name <- Sarajevo
        | City.Podgorica -> result.Name <- Podgorica
        | City.Tirana -> result.Name <- Tirana
        | City.Paris -> result.Name <- Paris
        | City.Rome -> result.Name <- Rome
        | City.Dublin -> result.Name <- Dublin
        | City.Bern -> result.Name <- Bern
        | City.Helsinki -> result.Name <- Helsinki
        | City.Hague -> result.Name <- Hague
        | City.Ljubljana -> result.Name <- Ljubljana

        result

    let toInternal (city: External.City) =
        match city.Name with
        | Belgrade -> City.Belgrade |> Ok
        | Berlin -> City.Berlin |> Ok
        | Budapest -> City.Budapest |> Ok
        | Sarajevo -> City.Sarajevo |> Ok
        | Podgorica -> City.Podgorica |> Ok
        | Tirana -> City.Tirana |> Ok
        | Paris -> City.Paris |> Ok
        | Rome -> City.Rome |> Ok
        | Dublin -> City.Dublin |> Ok
        | Bern -> City.Bern |> Ok
        | Helsinki -> City.Helsinki |> Ok
        | Hague -> City.Hague |> Ok
        | Ljubljana -> City.Ljubljana |> Ok
        | _ -> Error <| NotSupported $"City %s{city.Name}."

module Country =
    [<Literal>]
    let Serbia = nameof Country.Serbia

    [<Literal>]
    let Germany = nameof Country.Germany

    [<Literal>]
    let Bosnia = nameof Country.Bosnia

    [<Literal>]
    let Montenegro = nameof Country.Montenegro

    [<Literal>]
    let Albania = nameof Country.Albania

    [<Literal>]
    let Hungary = nameof Country.Hungary

    [<Literal>]
    let Ireland = nameof Country.Ireland

    [<Literal>]
    let Switzerland = nameof Country.Switzerland

    [<Literal>]
    let Finland = nameof Country.Finland

    [<Literal>]
    let France = nameof Country.France

    [<Literal>]
    let Netherlands = nameof Country.Netherlands

    [<Literal>]
    let Slovenia = nameof Country.Slovenia

    let toExternal country =
        let result = External.Country()

        let city =
            match country with
            | Country.Serbia city ->
                result.Name <- Serbia
                city
            | Country.Germany city ->
                result.Name <- Germany
                city
            | Country.Bosnia city ->
                result.Name <- Bosnia
                city
            | Country.Montenegro city ->
                result.Name <- Montenegro
                city
            | Country.Albania city ->
                result.Name <- Albania
                city
            | Country.Hungary city ->
                result.Name <- Hungary
                city
            | Country.Ireland city ->
                result.Name <- Ireland
                city
            | Country.Switzerland city ->
                result.Name <- Switzerland
                city
            | Country.Finland city ->
                result.Name <- Finland
                city
            | Country.France city ->
                result.Name <- France
                city
            | Country.Netherlands city ->
                result.Name <- Netherlands
                city
            | Country.Slovenia city ->
                result.Name <- Slovenia
                city

        result.City <- city |> City.toExternal

        result

    let toInternal (country: External.Country) =
        country.City
        |> City.toInternal
        |> Result.bind (fun city ->
            match country.Name with
            | Serbia -> Country.Serbia city |> Ok
            | Germany -> Country.Germany city |> Ok
            | Bosnia -> Country.Bosnia city |> Ok
            | Montenegro -> Country.Montenegro city |> Ok
            | Albania -> Country.Albania city |> Ok
            | Hungary -> Country.Hungary city |> Ok
            | Ireland -> Country.Ireland city |> Ok
            | Switzerland -> Country.Switzerland city |> Ok
            | Finland -> Country.Finland city |> Ok
            | France -> Country.France city |> Ok
            | Netherlands -> Country.Netherlands city |> Ok
            | Slovenia -> Country.Slovenia city |> Ok
            | _ -> Error <| NotSupported $"Country %s{country.Name}.")

module Embassy =
    [<Literal>]
    let Russian = nameof Embassy.Russian

    [<Literal>]
    let Spanish = nameof Embassy.Spanish

    [<Literal>]
    let Italian = nameof Embassy.Italian

    [<Literal>]
    let French = nameof Embassy.French

    [<Literal>]
    let German = nameof Embassy.German

    [<Literal>]
    let British = nameof Embassy.British

    let toExternal embassy =
        let result = External.Embassy()

        let country =
            match embassy with
            | Embassy.Russian country ->
                result.Name <- Russian
                country
            | Embassy.Spanish country ->
                result.Name <- Spanish
                country
            | Embassy.Italian country ->
                result.Name <- Italian
                country
            | Embassy.French country ->
                result.Name <- French
                country
            | Embassy.German country ->
                result.Name <- German
                country
            | Embassy.British country ->
                result.Name <- British
                country

        result.Country <- country |> Country.toExternal
        result

    let toInternal (embassy: External.Embassy) =
        embassy.Country
        |> Country.toInternal
        |> Result.bind (fun country ->
            match embassy.Name with
            | Russian -> Embassy.Russian country |> Ok
            | Spanish -> Embassy.Spanish country |> Ok
            | Italian -> Embassy.Italian country |> Ok
            | French -> Embassy.French country |> Ok
            | German -> Embassy.German country |> Ok
            | British -> Embassy.British country |> Ok
            | _ -> Error <| NotSupported $"Embassy %s{embassy.Name}.")

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
    let FirstAvailable = nameof ConfirmationOption.FirstAvailable

    [<Literal>]
    let LastAvailable = nameof ConfirmationOption.LastAvailable

    [<Literal>]
    let DateTimeRange = nameof ConfirmationOption.DateTimeRange

    let toExternal option =
        let result = External.ConfirmationOption()

        match option with
        | ConfirmationOption.FirstAvailable -> result.Type <- FirstAvailable
        | ConfirmationOption.LastAvailable -> result.Type <- LastAvailable
        | ConfirmationOption.DateTimeRange(min, max) ->
            result.Type <- DateTimeRange
            result.DateStart <- Nullable min
            result.DateEnd <- Nullable max

        result

    let toInternal (option: External.ConfirmationOption) =
        match option.Type with
        | FirstAvailable -> ConfirmationOption.FirstAvailable |> Ok
        | LastAvailable -> ConfirmationOption.LastAvailable |> Ok
        | DateTimeRange ->
            match option.DateStart |> Option.ofNullable, option.DateEnd |> Option.ofNullable with
            | Some min, Some max -> ConfirmationOption.DateTimeRange(min, max) |> Ok
            | _ -> Error <| NotFound "DateStart or DateEnd."
        | _ -> Error <| NotSupported $"ConfirmationOption %s{option.Type}."

module ConfirmationState =

    [<Literal>]
    let Disabled = nameof ConfirmationState.Disabled

    [<Literal>]
    let Manual = nameof ConfirmationState.Manual

    [<Literal>]
    let Auto = nameof ConfirmationState.Auto

    let toExternal (state: ConfirmationState) =
        let result = External.ConfirmationState()

        match state with
        | ConfirmationState.Disabled -> result.Type <- Disabled
        | ConfirmationState.Manual appointmentId ->
            result.Type <- Manual
            result.AppointmentId <- Some appointmentId.Value
        | ConfirmationState.Auto option ->
            result.Type <- Auto
            result.Option <- Some option |> Option.map ConfirmationOption.toExternal

        result

    let toInternal (state: External.ConfirmationState) =
        match state.Type with
        | Disabled -> ConfirmationState.Disabled |> Ok
        | Manual ->
            match state.AppointmentId with
            | Some id -> id |> AppointmentId |> ConfirmationState.Manual |> Ok
            | None -> Error <| NotFound "Appointment."
        | Auto ->
            match state.Option with
            | Some option -> option |> ConfirmationOption.toInternal |> Result.map ConfirmationState.Auto
            | None -> Error <| NotFound "ConfirmationOption."
        | _ -> Error <| NotSupported $"ConfirmationType %s{state.Type}."

module RequestState =
    [<Literal>]
    let Created = nameof ProcessState.Created

    [<Literal>]
    let InProcess = nameof ProcessState.InProcess

    [<Literal>]
    let Completed = nameof ProcessState.Completed

    [<Literal>]
    let Failed = nameof ProcessState.Failed

    let toExternal state =
        let result = External.RequestState()

        match state with
        | ProcessState.Created -> result.Type <- Created
        | ProcessState.InProcess -> result.Type <- InProcess
        | ProcessState.Completed msg ->
            result.Type <- Completed
            result.Message <- Some msg
        | ProcessState.Failed error ->
            result.Type <- Failed
            result.Error <- error |> Mapper.Error.toExternal |> Some

        result

    let toInternal (state: External.RequestState) =
        match state.Type with
        | Created -> ProcessState.Created |> Ok
        | InProcess -> ProcessState.InProcess |> Ok
        | Completed ->
            let msg =
                match state.Message with
                | Some(AP.IsString value) -> value
                | _ -> "Message not found."

            ProcessState.Completed msg |> Ok
        | Failed ->
            match state.Error with
            | Some error -> error |> Mapper.Error.toInternal |> Result.map ProcessState.Failed
            | None -> Error <| NotSupported "Failed state without error"
        | _ -> Error <| NotSupported $"Request state %s{state.Type}."

module Request =
    let toExternal request =
        let result = External.Request()

        result.Id <- request.Id.Value
        result.Payload <- request.Payload
        result.Embassy <- request.Embassy |> Embassy.toExternal
        result.State <- request.ProcessState |> RequestState.toExternal
        result.Attempt <- request.Attempt
        result.ConfirmationState <- request.ConfirmationState |> ConfirmationState.toExternal
        result.Appointments <- request.Appointments |> Seq.map Appointment.toExternal |> Seq.toArray
        result.Description <- request.Description
        result.GroupBy <- request.GroupBy
        result.Modified <- request.Modified

        result

    let toInternal (request: External.Request) =
        let embassyRes = request.Embassy |> Embassy.toInternal
        let stateRes = request.State |> RequestState.toInternal
        let confirmationStateRes = request.ConfirmationState |> ConfirmationState.toInternal

        let appointments =
            request.Appointments |> Seq.map Appointment.toInternal |> Set.ofSeq

        stateRes
        |> Result.bind (fun state ->
            confirmationStateRes
            |> Result.bind (fun confirmationState ->
                embassyRes
                |> Result.map (fun embassy ->
                    { Id = RequestId(request.Id)
                      Payload = request.Payload
                      Embassy = embassy
                      ProcessState = state
                      Attempt = request.Attempt
                      ConfirmationState = confirmationState
                      Appointments = appointments
                      Description = request.Description
                      GroupBy = request.GroupBy
                      Modified = request.Modified })))
