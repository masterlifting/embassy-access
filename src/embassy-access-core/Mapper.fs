[<RequireQualifiedAccess>]
module EA.Core.Mapper

open System
open Infrastructure
open EA.Core.Domain

module City =
    [<Literal>]
    let BELGRADE = nameof City.Belgrade

    [<Literal>]
    let BERLIN = nameof City.Berlin

    [<Literal>]
    let BUDAPEST = nameof City.Budapest

    [<Literal>]
    let SARAJEVO = nameof City.Sarajevo

    [<Literal>]
    let PODGORICA = nameof City.Podgorica

    [<Literal>]
    let TIRANA = nameof City.Tirana

    [<Literal>]
    let PARIS = nameof City.Paris

    [<Literal>]
    let ROME = nameof City.Rome

    [<Literal>]
    let DUBLIN = nameof City.Dublin

    [<Literal>]
    let BERN = nameof City.Bern

    [<Literal>]
    let HELSINKI = nameof City.Helsinki

    [<Literal>]
    let HAGUE = nameof City.Hague

    [<Literal>]
    let LJUBLJANA = nameof City.Ljubljana

    let toExternal city =
        let result = External.City()

        match city with
        | City.Belgrade -> result.Name <- BELGRADE
        | City.Berlin -> result.Name <- BERLIN
        | City.Budapest -> result.Name <- BUDAPEST
        | City.Sarajevo -> result.Name <- SARAJEVO
        | City.Podgorica -> result.Name <- PODGORICA
        | City.Tirana -> result.Name <- TIRANA
        | City.Paris -> result.Name <- PARIS
        | City.Rome -> result.Name <- ROME
        | City.Dublin -> result.Name <- DUBLIN
        | City.Bern -> result.Name <- BERN
        | City.Helsinki -> result.Name <- HELSINKI
        | City.Hague -> result.Name <- HAGUE
        | City.Ljubljana -> result.Name <- LJUBLJANA

        result

    let toInternal (city: External.City) =
        match city.Name with
        | BELGRADE -> City.Belgrade |> Ok
        | BERLIN -> City.Berlin |> Ok
        | BUDAPEST -> City.Budapest |> Ok
        | SARAJEVO -> City.Sarajevo |> Ok
        | PODGORICA -> City.Podgorica |> Ok
        | TIRANA -> City.Tirana |> Ok
        | PARIS -> City.Paris |> Ok
        | ROME -> City.Rome |> Ok
        | DUBLIN -> City.Dublin |> Ok
        | BERN -> City.Bern |> Ok
        | HELSINKI -> City.Helsinki |> Ok
        | HAGUE -> City.Hague |> Ok
        | LJUBLJANA -> City.Ljubljana |> Ok
        | _ -> Error <| NotSupported $"City %s{city.Name}."

module Country =
    [<Literal>]
    let SERBIA = nameof Country.Serbia

    [<Literal>]
    let GERMANY = nameof Country.Germany

    [<Literal>]
    let BOSNIA = nameof Country.Bosnia

    [<Literal>]
    let MONTENEGRO = nameof Country.Montenegro

    [<Literal>]
    let ALBANIA = nameof Country.Albania

    [<Literal>]
    let HUNGARY = nameof Country.Hungary

    [<Literal>]
    let IRELAND = nameof Country.Ireland

    [<Literal>]
    let SWITZERLAND = nameof Country.Switzerland

    [<Literal>]
    let FINLAND = nameof Country.Finland

    [<Literal>]
    let FRANCE = nameof Country.France

    [<Literal>]
    let NETHERLANDS = nameof Country.Netherlands

    [<Literal>]
    let SLOVENIA = nameof Country.Slovenia

    [<Literal>]
    let ITALY = nameof Country.Italy

    let toExternal country =
        let result = External.Country()

        let city =
            match country with
            | Country.Serbia city ->
                result.Name <- SERBIA
                city
            | Country.Germany city ->
                result.Name <- GERMANY
                city
            | Country.Bosnia city ->
                result.Name <- BOSNIA
                city
            | Country.Montenegro city ->
                result.Name <- MONTENEGRO
                city
            | Country.Albania city ->
                result.Name <- ALBANIA
                city
            | Country.Hungary city ->
                result.Name <- HUNGARY
                city
            | Country.Ireland city ->
                result.Name <- IRELAND
                city
            | Country.Switzerland city ->
                result.Name <- SWITZERLAND
                city
            | Country.Finland city ->
                result.Name <- FINLAND
                city
            | Country.France city ->
                result.Name <- FRANCE
                city
            | Country.Netherlands city ->
                result.Name <- NETHERLANDS
                city
            | Country.Slovenia city ->
                result.Name <- SLOVENIA
                city
            | Country.Italy city ->
                result.Name <- ITALY
                city

        result.City <- city |> City.toExternal

        result

    let toInternal (country: External.Country) =
        country.City
        |> City.toInternal
        |> Result.bind (fun city ->
            match country.Name with
            | SERBIA -> Country.Serbia city |> Ok
            | GERMANY -> Country.Germany city |> Ok
            | BOSNIA -> Country.Bosnia city |> Ok
            | MONTENEGRO -> Country.Montenegro city |> Ok
            | ALBANIA -> Country.Albania city |> Ok
            | HUNGARY -> Country.Hungary city |> Ok
            | IRELAND -> Country.Ireland city |> Ok
            | SWITZERLAND -> Country.Switzerland city |> Ok
            | FINLAND -> Country.Finland city |> Ok
            | FRANCE -> Country.France city |> Ok
            | NETHERLANDS -> Country.Netherlands city |> Ok
            | SLOVENIA -> Country.Slovenia city |> Ok
            | _ -> Error <| NotSupported $"Country %s{country.Name}.")

module Embassy =
    [<Literal>]
    let RUSSIAN = nameof Embassy.Russian

    [<Literal>]
    let SPANISH = nameof Embassy.Spanish

    [<Literal>]
    let ITALIAN = nameof Embassy.Italian

    [<Literal>]
    let FRENCH = nameof Embassy.French

    [<Literal>]
    let GERMAN = nameof Embassy.German

    [<Literal>]
    let BRITISH = nameof Embassy.British

    let toExternal embassy =
        let result = External.Embassy()

        let country =
            match embassy with
            | Embassy.Russian country ->
                result.Name <- RUSSIAN
                country
            | Embassy.Spanish country ->
                result.Name <- SPANISH
                country
            | Embassy.Italian country ->
                result.Name <- ITALIAN
                country
            | Embassy.French country ->
                result.Name <- FRENCH
                country
            | Embassy.German country ->
                result.Name <- GERMAN
                country
            | Embassy.British country ->
                result.Name <- BRITISH
                country

        result.Country <- country |> Country.toExternal
        result

    let toInternal (embassy: External.Embassy) =
        embassy.Country
        |> Country.toInternal
        |> Result.bind (fun country ->
            match embassy.Name with
            | RUSSIAN -> Embassy.Russian country |> Ok
            | SPANISH -> Embassy.Spanish country |> Ok
            | ITALIAN -> Embassy.Italian country |> Ok
            | FRENCH -> Embassy.French country |> Ok
            | GERMAN -> Embassy.German country |> Ok
            | BRITISH -> Embassy.British country |> Ok
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
    let FIRST_AVAILABLE = nameof ConfirmationOption.FirstAvailable

    [<Literal>]
    let LAST_AVAILABLE = nameof ConfirmationOption.LastAvailable

    [<Literal>]
    let DATE_TIME_RANGE = nameof ConfirmationOption.DateTimeRange

    let toExternal option =
        let result = External.ConfirmationOption()

        match option with
        | ConfirmationOption.FirstAvailable -> result.Type <- FIRST_AVAILABLE
        | ConfirmationOption.LastAvailable -> result.Type <- LAST_AVAILABLE
        | ConfirmationOption.DateTimeRange(min, max) ->
            result.Type <- DATE_TIME_RANGE
            result.DateStart <- Nullable min
            result.DateEnd <- Nullable max

        result

    let toInternal (option: External.ConfirmationOption) =
        match option.Type with
        | FIRST_AVAILABLE -> ConfirmationOption.FirstAvailable |> Ok
        | LAST_AVAILABLE -> ConfirmationOption.LastAvailable |> Ok
        | DATE_TIME_RANGE ->
            match option.DateStart |> Option.ofNullable, option.DateEnd |> Option.ofNullable with
            | Some min, Some max -> ConfirmationOption.DateTimeRange(min, max) |> Ok
            | _ -> Error <| NotFound "DateStart or DateEnd."
        | _ -> Error <| NotSupported $"ConfirmationOption %s{option.Type}."

module ConfirmationState =

    [<Literal>]
    let DISABLED = nameof ConfirmationState.Disabled

    [<Literal>]
    let MANUAL = nameof ConfirmationState.Manual

    [<Literal>]
    let AUTO = nameof ConfirmationState.Auto

    let toExternal (state: ConfirmationState) =
        let result = External.ConfirmationState()

        match state with
        | ConfirmationState.Disabled -> result.Type <- DISABLED
        | ConfirmationState.Manual appointmentId ->
            result.Type <- MANUAL
            result.AppointmentId <- Some appointmentId.Value
        | ConfirmationState.Auto option ->
            result.Type <- AUTO
            result.ConfirmationOption <- Some option |> Option.map ConfirmationOption.toExternal

        result

    let toInternal (state: External.ConfirmationState) =
        match state.Type with
        | DISABLED -> ConfirmationState.Disabled |> Ok
        | MANUAL ->
            match state.AppointmentId with
            | Some id -> id |> AppointmentId |> ConfirmationState.Manual |> Ok
            | None -> Error <| NotFound "Appointment."
        | AUTO ->
            match state.ConfirmationOption with
            | Some option -> option |> ConfirmationOption.toInternal |> Result.map ConfirmationState.Auto
            | None -> Error <| NotFound "ConfirmationOption."
        | _ -> Error <| NotSupported $"ConfirmationType %s{state.Type}."

module ProcessState =
    [<Literal>]
    let CREATED = nameof ProcessState.Created

    [<Literal>]
    let IN_PROCESS = nameof ProcessState.InProcess

    [<Literal>]
    let COMPLETED = nameof ProcessState.Completed

    [<Literal>]
    let FAILED = nameof ProcessState.Failed

    let toExternal state =
        let result = External.ProcessState()

        match state with
        | ProcessState.Created -> result.Type <- CREATED
        | ProcessState.InProcess -> result.Type <- IN_PROCESS
        | ProcessState.Completed msg ->
            result.Type <- COMPLETED
            result.Message <- Some msg
        | ProcessState.Failed error ->
            result.Type <- FAILED
            result.Error <- error |> Mapper.Error.toExternal |> Some

        result

    let toInternal (state: External.ProcessState) =
        match state.Type with
        | CREATED -> ProcessState.Created |> Ok
        | IN_PROCESS -> ProcessState.InProcess |> Ok
        | COMPLETED ->
            let msg =
                match state.Message with
                | Some(AP.IsString value) -> value
                | _ -> "Message not found."

            ProcessState.Completed msg |> Ok
        | FAILED ->
            match state.Error with
            | Some error -> error |> Mapper.Error.toInternal |> Result.map ProcessState.Failed
            | None -> Error <| NotSupported "Failed state without error"
        | _ -> Error <| NotSupported $"Request state %s{state.Type}."

module Service =
    let toExternal service =
        let result = External.Service()

        result.Name <- service.Name
        result.Payload <- service.Payload
        result.Embassy <- service.Embassy |> Embassy.toExternal
        result.Description <- service.Description

        result

    let toInternal (service: External.Service) =
        service.Embassy
        |> Embassy.toInternal
        |> Result.map (fun embassy ->
            { Name = service.Name
              Payload = service.Payload
              Embassy = embassy
              Description = service.Description })

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

            let! service = request.Service |> Service.toInternal
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
