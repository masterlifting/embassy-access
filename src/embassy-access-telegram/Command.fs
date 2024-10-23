[<RequireQualifiedAccess>]
module EA.Telegram.Command

module private List =
    [<Literal>]
    let Start = "/start"

    [<Literal>]
    let Mine = "/mine"

    [<Literal>]
    let Countries = "/countries"

    [<Literal>]
    let Cities = "/cities"

    [<Literal>]
    let UserCountries = "/user_countries"

    [<Literal>]
    let UserCities = "/user_cities"

    [<Literal>]
    let SubscriptionRequest = "/subscription_request"

    [<Literal>]
    let Subscribe = "/subscribe"

    [<Literal>]
    let UserSubscriptions = "/user_subscriptions"

    [<Literal>]
    let ConfirmAppointment = "/confirm_appointment"

type Name =
    | Start
    | Mine
    | Countries of string
    | UserCountries of string
    | Cities of string * string
    | UserCities of string * string
    | SubscriptionRequest of string * string * string
    | Subscribe of string * string * string * string
    | UserSubscriptions of string * string * string
    | ConfirmAppointment of string * string * string * string

let private build args = args |> String.concat "|"

let set command =
    match command with
    | Start -> List.Start
    | Mine -> List.Mine
    | Countries embassy -> [ List.Countries; embassy ] |> build
    | Cities(embassy, country) -> [ List.Cities; embassy; country ] |> build
    | UserCountries embassy -> [ List.UserCountries; embassy ] |> build
    | UserCities(embassy, country) -> [ List.UserCities; embassy; country ] |> build
    | UserSubscriptions(embassy, country, city) -> [ List.UserSubscriptions; embassy; country; city ] |> build
    | SubscriptionRequest(embassy, country, city) -> [ List.SubscriptionRequest; embassy; country; city ] |> build
    | Subscribe(embassy, country, city, payload) -> [ List.Subscribe; embassy; country; city; payload ] |> build
    | ConfirmAppointment(embassy, country, city, payload) ->
        [ List.ConfirmAppointment; embassy; country; city; payload ] |> build

let tryFind (value: string) =
    let parts = value.Split '|'

    match parts.Length with
    | 0 -> None
    | _ ->
        let argsLength = parts.Length - 1

        match parts[0] with
        | List.Start -> Some Start
        | List.Mine -> Some Mine
        | List.Countries ->
            match argsLength with
            | 1 -> Some(Countries(parts[1]))
            | _ -> None
        | List.Cities ->
            match argsLength with
            | 2 -> Some(Cities(parts[1], parts[2]))
            | _ -> None
        | List.UserCountries ->
            match argsLength with
            | 1 -> Some(UserCountries(parts[1]))
            | _ -> None
        | List.UserCities ->
            match argsLength with
            | 2 -> Some(UserCities(parts[1], parts[2]))
            | _ -> None
        | List.SubscriptionRequest ->
            match argsLength with
            | 3 -> Some(SubscriptionRequest(parts[1], parts[2], parts[3]))
            | _ -> None
        | List.Subscribe ->
            match argsLength with
            | 4 -> Some(Subscribe(parts[1], parts[2], parts[3], parts[4]))
            | _ -> None
        | List.UserSubscriptions ->
            match argsLength with
            | 3 -> Some(UserSubscriptions(parts[1], parts[2], parts[3]))
            | _ -> None
        | List.ConfirmAppointment ->
            match argsLength with
            | 4 -> Some(ConfirmAppointment(parts[1], parts[2], parts[3], parts[4]))
            | _ -> None
        | _ -> None
