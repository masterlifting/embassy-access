[<AutoOpen>]
module EA.Telegram.Domain.Culture

open System.Globalization

type Culture =
    | English of CultureInfo
    | Russian of CultureInfo

    static member create value =
        match value with
        | "ru-RU" ->
            match Constants.SUPPORTED_CULTURES.Contains value with
            | true -> Russian <| CultureInfo(value)
            | false -> English <| CultureInfo(Constants.EN_US_CULTURE)
        | _ -> English <| CultureInfo(Constants.EN_US_CULTURE)

    static member createDefault() =
        CultureInfo.CurrentCulture.Name |> Culture.create
