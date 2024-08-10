module internal EmbassyAccess.Mapper

open System
open Infrastructure
open EmbassyAccess.Domain
open EmbassyAccess.SerDe

let private _requestStateConverter = Json.Converter.RequestState()

let private _cities =
    [ Constant.City.Belgrade, Belgrade
      Constant.City.Berlin, Berlin
      Constant.City.Budapest, Budapest
      Constant.City.Sarajevo, Sarajevo
      Constant.City.Podgorica, Podgorica
      Constant.City.Tirana, Tirana
      Constant.City.Paris, Paris
      Constant.City.Rome, Rome
      Constant.City.Dublin, Dublin
      Constant.City.Bern, Bern
      Constant.City.Helsinki, Helsinki
      Constant.City.Hague, Hague
      Constant.City.Ljubljana, Ljubljana ]
    |> Map

let private _countries =
    [ Constant.Country.Serbia, Serbia
      Constant.Country.Germany, Germany
      Constant.Country.Bosnia, Bosnia
      Constant.Country.Montenegro, Montenegro
      Constant.Country.Albania, Albania
      Constant.Country.Hungary, Hungary
      Constant.Country.Ireland, Ireland
      Constant.Country.Switzerland, Switzerland
      Constant.Country.Finland, Finland
      Constant.Country.Netherlands, Netherlands
      Constant.Country.Slovenia, Slovenia
      Constant.Country.France, France ]
    |> Map

let private _embassies =
    [ Constant.Embassy.Russian, Russian
      Constant.Embassy.German, German
      Constant.Embassy.French, French
      Constant.Embassy.Italian, Italian
      Constant.Embassy.British, British ]
    |> Map

let toCity (city: External.City) : Result<City, Error'> =
    _cities
    |> Map.tryFind city.Name
    |> Option.map Ok
    |> Option.defaultValue (Error <| NotSupported $"City {city.Name}.")

let toCountry (country: External.Country) : Result<Country, Error'> =
    toCity country.City
    |> Result.bind (fun city ->
        _countries
        |> Map.tryFind country.Name
        |> Option.map (fun country -> Ok <| country city)
        |> Option.defaultValue (Error <| NotSupported $"Country {country.Name}."))

let toEmbassy (embassy: External.Embassy) : Result<Embassy, Error'> =
    toCountry embassy.Country
    |> Result.bind (fun country ->
        _embassies
        |> Map.tryFind embassy.Name
        |> Option.map (fun embassy -> Ok <| embassy country)
        |> Option.defaultValue (Error <| NotSupported $"Embassy {embassy.Name}."))

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
    state |> Json.deserialize'<RequestState> (Json.OptionType.DU _requestStateConverter)

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
        result.City <- country.City |> toCity

        result

    let toEmbassy (embassy: Embassy) : External.Embassy =
        let result = External.Embassy()

        result.Name <- embassy.Name
        result.Country <- embassy.Country |> toCountry

        result

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

        result.State <-
            match request.State |> Json.serialize' (Json.OptionType.DU _requestStateConverter) with
            | Ok x -> x
            | Error error -> failwith error.Message

        result.Embassy <- request.Embassy |> toEmbassy
        result.Appointments <- request.Appointments |> Seq.map toAppointment |> Seq.toArray
        result.Description <- request.Description |> Option.defaultValue ""
        result.Modified <- request.Modified

        result
