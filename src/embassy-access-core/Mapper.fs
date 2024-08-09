module internal EmbassyAccess.Mapper

open System
open Infrastructure
open EmbassyAccess.Domain

let toCity (city: External.City) : Result<City, Error'> =
    match city.Name with
    | Constants.City.Belgrade -> Ok Belgrade
    | Constants.City.Berlin -> Ok Berlin
    | Constants.City.Budapest -> Ok Budapest
    | Constants.City.Sarajevo -> Ok Sarajevo
    | Constants.City.Podgorica -> Ok Podgorica
    | Constants.City.Tirana -> Ok Tirana
    | Constants.City.Paris -> Ok Paris
    | Constants.City.Rome -> Ok Rome
    | Constants.City.Dublin -> Ok Dublin
    | Constants.City.Bern -> Ok Bern
    | Constants.City.Helsinki -> Ok Helsinki
    | Constants.City.Hague -> Ok Hague
    | Constants.City.Ljubljana -> Ok Ljubljana
    | _ -> Error <| NotSupported $"City {city.Name}."

let toCountry (country: External.Country) : Result<Country, Error'> =
    toCity country.City
    |> Result.bind (fun city ->
        match country.Name with
        | Constants.Country.Serbia -> Ok(Serbia city)
        | Constants.Country.Germany -> Ok(Germany city)
        | Constants.Country.Bosnia -> Ok(Bosnia city)
        | Constants.Country.Montenegro -> Ok(Montenegro city)
        | Constants.Country.Albania -> Ok(Albania city)
        | Constants.Country.Hungary -> Ok(Hungary city)
        | Constants.Country.Ireland -> Ok(Ireland city)
        | Constants.Country.Switzerland -> Ok(Switzerland city)
        | Constants.Country.Finland -> Ok(Finland city)
        | Constants.Country.Netherlands -> Ok(Netherlands city)
        | Constants.Country.Slovenia -> Ok(Slovenia city)
        | Constants.Country.France -> Ok(France city)
        | _ -> Error <| NotSupported $"Country {country.Name}.")


let toEmbassy (embassy: External.Embassy) : Result<Embassy, Error'> =
    toCountry embassy.Country
    |> Result.bind (fun country ->
        match embassy.Name with
        | Constants.Embassy.Russian -> Ok(Russian country)
        | Constants.Embassy.German -> Ok(German country)
        | Constants.Embassy.French -> Ok(French country)
        | Constants.Embassy.Italian -> Ok(Italian country)
        | Constants.Embassy.British -> Ok(British country)
        | _ -> Error <| NotSupported $"Embassy {embassy.Name}.")

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

let toRequestState (state: string) : Result<RequestState, Error'> =
    match state with
    | Constants.RequestState.Created -> Ok Created
    | Constants.RequestState.Running -> Ok Running
    | Constants.RequestState.Completed -> Ok Completed
    | Constants.RequestState.Failed -> Ok Failed
    | _ -> Error <| NotSupported $"Request state {state}."

let toRequest (request: External.Request) : Result<Request, Error'> =
    toEmbassy request.Embassy
    |> Result.bind (fun embassy ->
        request.State
        |> toRequestState
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

    let toCity (city: City) : External.City =
        let result = External.City()

        result.Name <- city.Name

        result

    let toCountry (country: Country) : External.Country =
        let result = External.Country()

        result.Name <- country.Name
        result.City <- toCity country.City

        result

    let toEmbassy (embassy: Embassy) : External.Embassy =
        let result = External.Embassy()

        result.Name <- embassy.Name
        result.Country <- toCountry embassy.Country

        result

    let toConfirmation (confirmation: Confirmation option) : External.Confirmation option =
        match confirmation with
        | None -> None
        | Some confirmation ->
            let result = External.Confirmation()
            result.Description <- confirmation.Description

            Some result

    let toAppointment (appointment: Appointment) : External.Appointment =
        let result = External.Appointment()

        result.Value <- appointment.Value
        result.DateTime <- appointment.Date.ToDateTime(appointment.Time)
        result.Description <- appointment.Description |> Option.defaultValue ""
        result.Confirmation <- appointment.Confirmation |> toConfirmation

        result

    let toRequest (request: Request) : External.Request =
        let result = External.Request()

        result.Id <- request.Id.Value
        result.Value <- request.Value
        result.Attempt <- request.Attempt
        result.State <- request.State.Name
        result.Embassy <- toEmbassy request.Embassy
        result.Appointments <- request.Appointments |> Seq.map toAppointment |> Seq.toArray
        result.Description <- request.Description |> Option.defaultValue ""
        result.Modified <- request.Modified

        result
