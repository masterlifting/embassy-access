[<RequireQualifiedAccess>]
module EA.Core.SerDe

open Infrastructure
open EA.Core.Domain

module City =
    //ISO 3166-1 alpha-3
    module private Code =
        [<Literal>]
        let BELGRADE = "BEG"

        [<Literal>]
        let BERLIN = "BER"

        [<Literal>]
        let BUDAPEST = "BUD"

        [<Literal>]
        let SARAJEVO = "SJJ"

        [<Literal>]
        let PODGORICA = "TGD"

        [<Literal>]
        let TIRANA = "TIA"

        [<Literal>]
        let PARIS = "PAR"

        [<Literal>]
        let ROME = "ROM"

        [<Literal>]
        let DUBLIN = "DUB"

        [<Literal>]
        let BERN = "BRN"

        [<Literal>]
        let HELSINKI = "HEL"

        [<Literal>]
        let HAGUE = "HAG"

        [<Literal>]
        let LJUBLJANA = "LJU"

    let serialize city =
        match city with
        | Belgrade -> Code.BELGRADE
        | Berlin -> Code.BERLIN
        | Budapest -> Code.BUDAPEST
        | Sarajevo -> Code.SARAJEVO
        | Podgorica -> Code.PODGORICA
        | Tirana -> Code.TIRANA
        | Paris -> Code.PARIS
        | Rome -> Code.ROME
        | Dublin -> Code.DUBLIN
        | Bern -> Code.BERN
        | Helsinki -> Code.HELSINKI
        | Hague -> Code.HAGUE
        | Ljubljana -> Code.LJUBLJANA

    let deserialize city =
        match city with
        | Code.BELGRADE -> Ok Belgrade
        | Code.BERLIN -> Ok Berlin
        | Code.BUDAPEST -> Ok Budapest
        | Code.SARAJEVO -> Ok Sarajevo
        | Code.PODGORICA -> Ok Podgorica
        | Code.TIRANA -> Ok Tirana
        | Code.PARIS -> Ok Paris
        | Code.ROME -> Ok Rome
        | Code.DUBLIN -> Ok Dublin
        | Code.BERN -> Ok Bern
        | Code.HELSINKI -> Ok Helsinki
        | Code.HAGUE -> Ok Hague
        | Code.LJUBLJANA -> Ok Ljubljana
        | _ ->
            Error
            <| NotSupported $"{city}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}"

module Country =

    [<Literal>]
    let private DELIMITER = ","

    //ISO 3166-1 alpha-3
    module private Code =
        [<Literal>]
        let SERBIA = "SRB"

        [<Literal>]
        let GERMANY = "DEU"

        [<Literal>]
        let BOSNIA = "BIH"

        [<Literal>]
        let MONTENEGRO = "MNE"

        [<Literal>]
        let ALBANIA = "ALB"

        [<Literal>]
        let HUNGARY = "HUN"

        [<Literal>]
        let IRELAND = "IRL"

        [<Literal>]
        let SWITZERLAND = "CHE"

        [<Literal>]
        let FINLAND = "FIN"

        [<Literal>]
        let FRANCE = "FRA"

        [<Literal>]
        let NETHERLANDS = "NLD"

        [<Literal>]
        let SLOVENIA = "SLO"

        [<Literal>]
        let ITALY = "ITA"

    let serialize country =
        match country with
        | Serbia city -> (Code.SERBIA, city)
        | Germany city -> (Code.GERMANY, city)
        | Bosnia city -> (Code.BOSNIA, city)
        | Montenegro city -> (Code.MONTENEGRO, city)
        | Albania city -> (Code.ALBANIA, city)
        | Hungary city -> (Code.HUNGARY, city)
        | Ireland city -> (Code.IRELAND, city)
        | Switzerland city -> (Code.SWITZERLAND, city)
        | Finland city -> (Code.FINLAND, city)
        | France city -> (Code.FRANCE, city)
        | Netherlands city -> (Code.NETHERLANDS, city)
        | Slovenia city -> (Code.SLOVENIA, city)
        | Italy city -> (Code.ITALY, city)
        |> fun (country, city) -> country + DELIMITER + City.serialize city

    let deserialize (value: string) =
        let parts = value.Split DELIMITER

        match parts.Length with
        | 2 ->
            let country = parts[0]
            let city = parts[1]

            city
            |> City.deserialize
            |> Result.bind (fun city ->
                match country with
                | Code.SERBIA -> Ok <| Serbia city
                | Code.GERMANY -> Ok <| Germany city
                | Code.BOSNIA -> Ok <| Bosnia city
                | Code.MONTENEGRO -> Ok <| Montenegro city
                | Code.ALBANIA -> Ok <| Albania city
                | Code.HUNGARY -> Ok <| Hungary city
                | Code.IRELAND -> Ok <| Ireland city
                | Code.SWITZERLAND -> Ok <| Switzerland city
                | Code.FINLAND -> Ok <| Finland city
                | Code.FRANCE -> Ok <| France city
                | Code.NETHERLANDS -> Ok <| Netherlands city
                | Code.SLOVENIA -> Ok <| Slovenia city
                | _ ->
                    Error
                    <| NotSupported
                        $"{country}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}")
        | _ ->
            Error
            <| NotSupported $"{value}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}"

module Embassy =

    [<Literal>]
    let private DELIMITER = ","

    //ISO 3166-1 alpha-2
    module private Code =
        [<Literal>]
        let RUSSIAN = "RU"

        [<Literal>]
        let SPANISH = "ES"

        [<Literal>]
        let ITALIAN = "IT"

        [<Literal>]
        let FRENCH = "FR"

        [<Literal>]
        let GERMAN = "DE"

        [<Literal>]
        let BRITISH = "GB"

    let serialize embassy =
        match embassy with
        | Russian country -> (Code.RUSSIAN, country)
        | Spanish country -> (Code.SPANISH, country)
        | Italian country -> (Code.ITALIAN, country)
        | French country -> (Code.FRENCH, country)
        | German country -> (Code.GERMAN, country)
        | British country -> (Code.BRITISH, country)
        |> fun (embassy, country) -> embassy + DELIMITER + Country.serialize country

    let deserialize (value: string) =
        let parts = value.Split DELIMITER

        match parts.Length > 1 with
        | true ->
            let embassy = parts[0]
            let countryValue = parts[1..] |> String.concat DELIMITER

            countryValue
            |> Country.deserialize
            |> Result.bind (fun country ->
                match embassy with
                | Code.RUSSIAN -> Ok <| Russian country
                | Code.SPANISH -> Ok <| Spanish country
                | Code.ITALIAN -> Ok <| Italian country
                | Code.FRENCH -> Ok <| French country
                | Code.GERMAN -> Ok <| German country
                | Code.BRITISH -> Ok <| British country
                | _ ->
                    Error
                    <| NotSupported
                        $"{embassy}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}")
        | false ->
            Error
            <| NotSupported $"{value}. {ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__)}"
