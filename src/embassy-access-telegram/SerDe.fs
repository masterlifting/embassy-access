module EA.Telegram.SerDe

open System
open Infrastructure
open EA.Domain

module City =
    //ISO 3166-1 alpha-3
    module private Code =
        [<Literal>]
        let Belgrade = "BEG"

        [<Literal>]
        let Berlin = "BER"

        [<Literal>]
        let Budapest = "BUD"

        [<Literal>]
        let Sarajevo = "SJJ"

        [<Literal>]
        let Podgorica = "TGD"

        [<Literal>]
        let Tirana = "TIA"

        [<Literal>]
        let Paris = "PAR"

        [<Literal>]
        let Rome = "ROM"

        [<Literal>]
        let Dublin = "DUB"

        [<Literal>]
        let Bern = "BRN"

        [<Literal>]
        let Helsinki = "HEL"

        [<Literal>]
        let Hague = "HAG"

        [<Literal>]
        let Ljubljana = "LJU"

    let serialize city =
        match city with
        | Belgrade -> Code.Belgrade
        | Berlin -> Code.Berlin
        | Budapest -> Code.Budapest
        | Sarajevo -> Code.Sarajevo
        | Podgorica -> Code.Podgorica
        | Tirana -> Code.Tirana
        | Paris -> Code.Paris
        | Rome -> Code.Rome
        | Dublin -> Code.Dublin
        | Bern -> Code.Bern
        | Helsinki -> Code.Helsinki
        | Hague -> Code.Hague
        | Ljubljana -> Code.Ljubljana

    let deserialize city =
        match city with
        | Code.Belgrade -> Ok Belgrade
        | Code.Berlin -> Ok Berlin
        | Code.Budapest -> Ok Budapest
        | Code.Sarajevo -> Ok Sarajevo
        | Code.Podgorica -> Ok Podgorica
        | Code.Tirana -> Ok Tirana
        | Code.Paris -> Ok Paris
        | Code.Rome -> Ok Rome
        | Code.Dublin -> Ok Dublin
        | Code.Bern -> Ok Bern
        | Code.Helsinki -> Ok Helsinki
        | Code.Hague -> Ok Hague
        | Code.Ljubljana -> Ok Ljubljana
        | _ ->
            Error
            <| Operation
                { Message = $"Unknown city symbol: {city}. EA.Telegram.SerDe.Embassy.toCity"
                  Code = None }

module Country =

    [<Literal>]
    let internal Delimiter = ","

    //ISO 3166-1 alpha-3
    module private Code =
        [<Literal>]
        let Serbia = "SRB"

        [<Literal>]
        let Germany = "DEU"

        [<Literal>]
        let Bosnia = "BIH"

        [<Literal>]
        let Montenegro = "MNE"

        [<Literal>]
        let Albania = "ALB"

        [<Literal>]
        let Hungary = "HUN"

        [<Literal>]
        let Ireland = "IRL"

        [<Literal>]
        let Switzerland = "CHE"

        [<Literal>]
        let Finland = "FIN"

        [<Literal>]
        let France = "FRA"

        [<Literal>]
        let Netherlands = "NLD"

        [<Literal>]
        let Slovenia = "SLO"

    let serialize country =
        match country with
        | Serbia(city) -> (Code.Serbia, city)
        | Germany(city) -> (Code.Germany, city)
        | Bosnia(city) -> (Code.Bosnia, city)
        | Montenegro(city) -> (Code.Montenegro, city)
        | Albania(city) -> (Code.Albania, city)
        | Hungary(city) -> (Code.Hungary, city)
        | Ireland(city) -> (Code.Ireland, city)
        | Switzerland(city) -> (Code.Switzerland, city)
        | Finland(city) -> (Code.Finland, city)
        | France(city) -> (Code.France, city)
        | Netherlands(city) -> (Code.Netherlands, city)
        | Slovenia(city) -> (Code.Slovenia, city)
        |> fun (country, city) -> country + Delimiter + City.serialize city

    let deserialize (value: string) =
        let parts = value.Split Delimiter

        match parts.Length with
        | 2 ->
            let country = parts[0]
            let city = parts[1]

            City.deserialize city
            |> Result.bind (fun city ->
                match country with
                | Code.Serbia -> Ok <| Serbia city
                | Code.Germany -> Ok <| Germany city
                | Code.Bosnia -> Ok <| Bosnia city
                | Code.Montenegro -> Ok <| Montenegro city
                | Code.Albania -> Ok <| Albania city
                | Code.Hungary -> Ok <| Hungary city
                | Code.Ireland -> Ok <| Ireland city
                | Code.Switzerland -> Ok <| Switzerland city
                | Code.Finland -> Ok <| Finland city
                | Code.France -> Ok <| France city
                | Code.Netherlands -> Ok <| Netherlands city
                | Code.Slovenia -> Ok <| Slovenia city
                | _ ->
                    Error
                    <| Operation
                        { Message = $"Unknown country code: {country}. EA.Telegram.SerDe.Embassy.toCountry"
                          Code = None })
        | _ ->
            Error
            <| Operation
                { Message = $"Invalid country format: {value}. EA.Telegram.SerDe.Embassy.deserialize"
                  Code = None }

module Embassy =

    [<Literal>]
    let internal Delimiter = ","

    //ISO 3166-1 alpha-2
    module private Code =
        [<Literal>]
        let Russian = "RU"

        [<Literal>]
        let Spanish = "ES"

        [<Literal>]
        let Italian = "IT"

        [<Literal>]
        let French = "FR"

        [<Literal>]
        let German = "DE"

        [<Literal>]
        let British = "GB"

    let serialize embassy =
        match embassy with
        | Russian(country) -> (Code.Russian, country)
        | Spanish(country) -> (Code.Spanish, country)
        | Italian(country) -> (Code.Italian, country)
        | French(country) -> (Code.French, country)
        | German(country) -> (Code.German, country)
        | British(country) -> (Code.British, country)
        |> fun (embassy, country) -> embassy + Delimiter + Country.serialize country

    let deserialize (value: string) =
        let parts = value.Split Delimiter

        match parts.Length with
        | 3 ->
            let embassy = parts[0]
            let country = parts[1]
            let city = parts[2]

            Country.deserialize (country + Country.Delimiter + city)
            |> Result.bind (fun country ->
                match embassy with
                | Code.Russian -> Ok <| Russian country
                | Code.Spanish -> Ok <| Spanish country
                | Code.Italian -> Ok <| Italian country
                | Code.French -> Ok <| French country
                | Code.German -> Ok <| German country
                | Code.British -> Ok <| British country
                | _ ->
                    Error
                    <| Operation
                        { Message = $"Unknown embassy code: {embassy}. EA.Telegram.SerDe.Embassy.deserialize"
                          Code = None })
        | _ ->
            Error
            <| Operation
                { Message = $"Invalid embassy format: {value}. EA.Telegram.SerDe.Embassy.deserialize"
                  Code = None }

module Command =

    open Infrastructure.Logging

    [<Literal>]
    let private Delimiter = "|"

    module private List =
        [<Literal>]
        let Start = "/start"

        [<Literal>]
        let Mine = "/mine"

        [<Literal>]
        let Countries = "/001"

        [<Literal>]
        let Cities = "/002"

        [<Literal>]
        let UserCountries = "/003"

        [<Literal>]
        let UserCities = "/004"

        [<Literal>]
        let SubscriptionRequest = "/005"

        [<Literal>]
        let Subscribe = "/006"

        [<Literal>]
        let UserSubscriptions = "/007"

        [<Literal>]
        let ConfirmAppointment = "/008"

    type Name =
        | Start
        | Mine
        | Countries of embassy: string
        | UserCountries of embassy: string
        | Cities of embassy: string * country: string
        | UserCities of embassy: string * country: string
        | SubscriptionRequest of Embassy
        | Subscribe of Embassy * payload: string
        | UserSubscriptions of Embassy
        | ConfirmAppointment of Embassy * AppointmentId

    let private build args = args |> String.concat Delimiter

    let private printSize (value: string) =
        let size = System.Text.Encoding.UTF8.GetByteCount(value)
        $"'{value}' -> {size}" |> Log.info

    let serialize command =
        match command with
        | Start -> List.Start
        | Mine -> List.Mine
        | Countries embassy -> [ List.Countries; embassy ] |> build
        | Cities(embassy, country) ->
            [ List.Cities; embassy; country ] |> build
        | UserCountries embassy -> [ List.UserCountries; embassy ] |> build
        | UserCities(embassy, country) -> [ List.UserCities; embassy; country ] |> build
        | UserSubscriptions embassy ->
            embassy
            |> Embassy.serialize
            |> fun embassy -> [ List.UserSubscriptions; embassy ] |> build
        | SubscriptionRequest embassy ->
            embassy
            |> Embassy.serialize
            |> fun embassy -> [ List.SubscriptionRequest; embassy ] |> build
        | Subscribe(embassy, payload) ->
            embassy
            |> Embassy.serialize
            |> fun embassy -> [ List.Subscribe; embassy; payload ] |> build
        | ConfirmAppointment(embassy, appointmentId) ->
            embassy
            |> Embassy.serialize
            |> fun embassy -> [ List.ConfirmAppointment; embassy; appointmentId.Value |> string ] |> build
        |> fun result ->
            result |> printSize
            result

    let deserialize (value: string) =
        let parts = value.Split Delimiter

        match parts.Length with
        | 0 -> Ok None
        | _ ->
            let argsLength = parts.Length - 1

            match parts[0] with
            | List.Start -> Ok <| Some Start
            | List.Mine -> Ok <| Some Mine
            | List.Countries ->
                match argsLength with
                | 1 -> Ok <| Some(Countries(parts[1]))
                | _ -> Ok <| None
            | List.Cities ->
                match argsLength with
                | 2 -> Ok <| Some(Cities(parts[1], parts[2]))
                | _ -> Ok <| None
            | List.UserCountries ->
                match argsLength with
                | 1 -> Ok <| Some(UserCountries(parts[1]))
                | _ -> Ok <| None
            | List.UserCities ->
                match argsLength with
                | 2 -> Ok <| Some(UserCities(parts[1], parts[2]))
                | _ -> Ok <| None
            | List.SubscriptionRequest ->
                match argsLength with
                | 1 ->
                    parts[1]
                    |> Embassy.deserialize
                    |> Result.map (fun embassy -> Some(SubscriptionRequest(embassy)))
                | _ -> Ok <| None
            | List.Subscribe ->
                match argsLength with
                | 2 ->
                    parts[1]
                    |> Embassy.deserialize
                    |> Result.map (fun embassy ->
                        let payload = parts[2]
                        Some(Subscribe(embassy, payload)))
                | _ -> Ok <| None
            | List.UserSubscriptions ->
                match argsLength with
                | 1 ->
                    parts[1]
                    |> Embassy.deserialize
                    |> Result.map (fun embassy -> Some(UserSubscriptions(embassy)))
                | _ -> Ok <| None
            | List.ConfirmAppointment ->
                match argsLength with
                | 2 ->
                    parts[1]
                    |> Embassy.deserialize
                    |> Result.map (fun embassy ->
                        let appointmentId = parts[2] |> Guid.Parse |> AppointmentId
                        Some(ConfirmAppointment(embassy, appointmentId)))
                | _ -> Ok <| None
            | _ -> Ok <| None
