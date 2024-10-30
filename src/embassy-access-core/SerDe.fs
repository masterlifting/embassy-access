[<RequireQualifiedAccess>]
module EA.SerDe

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
            <| NotSupported $"{city}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}"

module Country =

    [<Literal>]
    let private Delimiter = ","

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

            city
            |> City.deserialize
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
                    <| NotSupported
                        $"{country}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}")
        | _ ->
            Error
            <| NotSupported $"{value}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}"

module Embassy =

    [<Literal>]
    let private Delimiter = ","

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

        match parts.Length > 1 with
        | true ->
            let embassy = parts[0]
            let countryValue = parts[1..] |> String.concat Delimiter

            countryValue
            |> Country.deserialize
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
                    <| NotSupported
                        $"{embassy}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}")
        | false ->
            Error
            <| NotSupported $"{value}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}"
