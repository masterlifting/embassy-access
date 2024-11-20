module EA.Telegram.Command

open EA.Core.Domain
open Infrastructure.Logging

[<Literal>]
let private Delimiter = "|"

module private Code =
    [<Literal>]
    let START = "/start"

    [<Literal>]
    let MINE = "/mine"

    [<Literal>]
    let COUNTRIES = "/001"

    [<Literal>]
    let CITIES = "/002"

    [<Literal>]
    let USER_COUNTRIES = "/003"

    [<Literal>]
    let USER_CITIES = "/004"

    [<Literal>]
    let SUBSCRIPTION_REQUEST = "/005"

    [<Literal>]
    let USER_SUBSCRIPTIONS = "/007"

    [<Literal>]
    let CHOOSE_APPOINTMENT_REQUEST = "/008"

    [<Literal>]
    let CONFIRM_APPOINTMENT = "/009"

    [<Literal>]
    let REMOVE_SUBSCRIPTION = "/010"

    [<Literal>]
    let SUBSCRIBE_SEARCH_APPOINTMENTS = "/006"

    [<Literal>]
    let SUBSCRIBE_SEARCH_OTHERS = "/011"

    [<Literal>]
    let SUBSCRIBE_SEARCH_PASSPORT_RESULT = "/012"

    [<Literal>]
    let CHOOSE_SUBSCRIPTION_REQUEST = "/013"

    [<Literal>]
    let CHOOSE_SUBSCRIPTION_REQUEST_WAY = "/014"

type Name =
    | Embassies
    | Countries of embassyName: string
    | Cities of embassyName: string * countryName: string
    | UserEmbassies
    | UserCountries of embassyName: string
    | UserCities of embassyName: string * countryName: string
    | ChoseSubscriptionRequest of Embassy * command: string * way: string
    | ChoseSubscriptionRequestWay of Embassy * command: string
    | SubscriptionRequest of Embassy
    | Services of Embassy
    | SubscribeSearchAppointments of Embassy * payload: string
    | SubscribeSearchOthers of Embassy * payload: string
    | SubscribeSearchPassportResult of Embassy * payload: string
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
    | Embassies -> Code.START
    | UserEmbassies -> Code.MINE
    | Countries embassyName -> [ Code.COUNTRIES; embassyName ] |> build
    | Cities(embassyName, countryName) -> [ Code.CITIES; embassyName; countryName ] |> build
    | UserCountries embassyName -> [ Code.USER_COUNTRIES; embassyName ] |> build
    | UserCities(embassyName, countryName) -> [ Code.USER_CITIES; embassyName; countryName ] |> build
    | UserSubscriptions embassy ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.USER_SUBSCRIPTIONS; embassy ] |> build
    | ChoseSubscriptionRequestWay(embassy, command) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.CHOOSE_SUBSCRIPTION_REQUEST_WAY; embassy; command ] |> build
    | ChoseSubscriptionRequest(embassy, command, way) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.CHOOSE_SUBSCRIPTION_REQUEST; embassy; command; way ] |> build
    | SubscriptionRequest embassy ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SUBSCRIPTION_REQUEST; embassy ] |> build
    | SubscribeSearchAppointments(embassy, payload) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SUBSCRIBE_SEARCH_APPOINTMENTS; embassy; payload ] |> build
    | SubscribeSearchOthers(embassy, payload) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SUBSCRIBE_SEARCH_OTHERS; embassy; payload ] |> build
    | SubscribeSearchPassportResult(embassy, payload) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SUBSCRIBE_SEARCH_PASSPORT_RESULT; embassy; payload ] |> build
    | ChooseAppointmentRequest(embassy, appointmentId) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy ->
            [ Code.CHOOSE_APPOINTMENT_REQUEST; embassy; appointmentId.Value |> string ]
            |> build
    | ConfirmAppointment(requestId, appointmentId) ->
        [ Code.CONFIRM_APPOINTMENT
          requestId.Value |> string
          appointmentId.Value |> string ]
        |> build
    | RemoveSubscription requestId -> [ Code.REMOVE_SUBSCRIPTION; requestId.Value |> string ] |> build

let get (value: string) =
    let parts = value.Split Delimiter

    match parts.Length with
    | 0 -> Ok None
    | _ ->
        let argsLength = parts.Length - 1

        match parts[0] with
        | Code.START -> Ok <| Some Embassies
        | Code.MINE -> Ok <| Some UserEmbassies
        | Code.COUNTRIES ->
            match argsLength with
            | 1 -> Ok <| Some(Countries(parts[1]))
            | _ -> Ok <| None
        | Code.CITIES ->
            match argsLength with
            | 2 -> Ok <| Some(Cities(parts[1], parts[2]))
            | _ -> Ok <| None
        | Code.USER_COUNTRIES ->
            match argsLength with
            | 1 -> Ok <| Some(UserCountries(parts[1]))
            | _ -> Ok <| None
        | Code.USER_CITIES ->
            match argsLength with
            | 2 -> Ok <| Some(UserCities(parts[1], parts[2]))
            | _ -> Ok <| None
        | Code.CHOOSE_SUBSCRIPTION_REQUEST_WAY ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let command = parts[2]
                    Some(ChoseSubscriptionRequestWay(embassy, command)))
            | _ -> Ok <| None
        | Code.CHOOSE_SUBSCRIPTION_REQUEST ->
            match argsLength with
            | 3 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let command = parts[2]
                    let way = parts[3]
                    Some(ChoseSubscriptionRequest(embassy, command, way)))
            | _ -> Ok <| None
        | Code.SUBSCRIPTION_REQUEST ->
            match argsLength with
            | 1 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (Some << SubscriptionRequest)
            | _ -> Ok <| None
        | Code.SUBSCRIBE_SEARCH_APPOINTMENTS ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let payload = parts[2]
                    Some(SubscribeSearchAppointments(embassy, payload)))
            | _ -> Ok <| None
        | Code.SUBSCRIBE_SEARCH_OTHERS ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let payload = parts[2]
                    Some(SubscribeSearchOthers(embassy, payload)))
            | _ -> Ok <| None
        | Code.SUBSCRIBE_SEARCH_PASSPORT_RESULT ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let payload = parts[2]
                    Some(SubscribeSearchPassportResult(embassy, payload)))
            | _ -> Ok <| None
        | Code.USER_SUBSCRIPTIONS ->
            match argsLength with
            | 1 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (Some << UserSubscriptions)
            | _ -> Ok <| None
        | Code.CHOOSE_APPOINTMENT_REQUEST ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.bind (fun embassy ->
                    parts[2]
                    |> AppointmentId.create
                    |> Result.map (fun appointmentId -> Some(ChooseAppointmentRequest(embassy, appointmentId))))
            | _ -> Ok <| None
        | Code.CONFIRM_APPOINTMENT ->
            match argsLength with
            | 2 ->
                parts[1]
                |> RequestId.create
                |> Result.bind (fun requestId ->
                    parts[2]
                    |> AppointmentId.create
                    |> Result.map (fun appointmentId -> Some(ConfirmAppointment(requestId, appointmentId))))
            | _ -> Ok <| None
        | Code.REMOVE_SUBSCRIPTION ->
            match argsLength with
            | 1 -> parts[1] |> RequestId.create |> Result.map (Some << RemoveSubscription)
            | _ -> Ok <| None
        | _ -> Ok <| None
