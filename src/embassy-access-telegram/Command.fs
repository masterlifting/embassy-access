module EA.Telegram.Command

open EA.Domain
open Infrastructure.Logging

[<Literal>]
let private Delimiter = "|"

module private Code =
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
    let ChooseAppointmentRequest = "/008"

    [<Literal>]
    let ConfirmAppointment = "/009"

    [<Literal>]
    let RemoveSubscription = "/010"

type Name =
    | Start
    | Mine
    | Countries of embassyName: string
    | UserCountries of embassyName: string
    | Cities of embassyName: string * countryName: string
    | UserCities of embassyName: string * countryName: string
    | SubscriptionRequest of Embassy
    | Subscribe of Embassy * payload: string
    | UserSubscriptions of Embassy
    | ChooseAppointmentRequest of Embassy * AppointmentId
    | ConfirmAppointment of RequestId * AppointmentId
    | RemoveSubscription of RequestId

let private build args = args |> String.concat Delimiter

let private printSize (value: string) =
    let size = System.Text.Encoding.UTF8.GetByteCount(value)
    $"'{value}' -> {size}" |> Log.info
    value

let set command =
    match command with
    | Start -> Code.Start
    | Mine -> Code.Mine
    | Countries embassyName -> [ Code.Countries; embassyName ] |> build
    | Cities(embassyName, countryName) -> [ Code.Cities; embassyName; countryName ] |> build
    | UserCountries embassyName -> [ Code.UserCountries; embassyName ] |> build
    | UserCities(embassyName, countryName) -> [ Code.UserCities; embassyName; countryName ] |> build
    | UserSubscriptions embassy ->
        embassy
        |> EA.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.UserSubscriptions; embassy ] |> build
    | SubscriptionRequest embassy ->
        embassy
        |> EA.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SubscriptionRequest; embassy ] |> build
    | Subscribe(embassy, payload) ->
        embassy
        |> EA.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.Subscribe; embassy; payload ] |> build
    | ChooseAppointmentRequest(embassy, appointmentId) ->
        embassy
        |> EA.SerDe.Embassy.serialize
        |> fun embassy ->
            [ Code.ChooseAppointmentRequest; embassy; appointmentId.Value |> string ]
            |> build
    | ConfirmAppointment(requestId, appointmentId) ->
        [ Code.ConfirmAppointment
          requestId.Value |> string
          appointmentId.Value |> string ]
        |> build
    | RemoveSubscription requestId -> [ Code.RemoveSubscription; requestId.Value |> string ] |> build

let get (value: string) =
    let parts = value.Split Delimiter

    match parts.Length with
    | 0 -> Ok None
    | _ ->
        let argsLength = parts.Length - 1

        match parts[0] with
        | Code.Start -> Ok <| Some Start
        | Code.Mine -> Ok <| Some Mine
        | Code.Countries ->
            match argsLength with
            | 1 -> Ok <| Some(Countries(parts[1]))
            | _ -> Ok <| None
        | Code.Cities ->
            match argsLength with
            | 2 -> Ok <| Some(Cities(parts[1], parts[2]))
            | _ -> Ok <| None
        | Code.UserCountries ->
            match argsLength with
            | 1 -> Ok <| Some(UserCountries(parts[1]))
            | _ -> Ok <| None
        | Code.UserCities ->
            match argsLength with
            | 2 -> Ok <| Some(UserCities(parts[1], parts[2]))
            | _ -> Ok <| None
        | Code.SubscriptionRequest ->
            match argsLength with
            | 1 ->
                parts[1]
                |> EA.SerDe.Embassy.deserialize
                |> Result.map (Some << SubscriptionRequest)
            | _ -> Ok <| None
        | Code.Subscribe ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let payload = parts[2]
                    Some(Subscribe(embassy, payload)))
            | _ -> Ok <| None
        | Code.UserSubscriptions ->
            match argsLength with
            | 1 ->
                parts[1]
                |> EA.SerDe.Embassy.deserialize
                |> Result.map (Some << UserSubscriptions)
            | _ -> Ok <| None
        | Code.ChooseAppointmentRequest ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.SerDe.Embassy.deserialize
                |> Result.bind (fun embassy ->
                    parts[2]
                    |> AppointmentId.create
                    |> Result.map (fun appointmentId -> Some(ChooseAppointmentRequest(embassy, appointmentId))))
            | _ -> Ok <| None
        | Code.ConfirmAppointment ->
            match argsLength with
            | 2 ->
                parts[1]
                |> RequestId.create
                |> Result.bind (fun requestId ->
                    parts[2]
                    |> AppointmentId.create
                    |> Result.map (fun appointmentId -> Some(ConfirmAppointment(requestId, appointmentId))))
            | _ -> Ok <| None
        | Code.RemoveSubscription ->
            match argsLength with
            | 1 -> parts[1] |> RequestId.create |> Result.map (Some << RemoveSubscription)
            | _ -> Ok <| None
        | _ -> Ok <| None
