module EA.Telegram.Command

open EA.Core.Domain
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
    let UserSubscriptions = "/007"

    [<Literal>]
    let ChooseAppointmentRequest = "/008"

    [<Literal>]
    let ConfirmAppointment = "/009"

    [<Literal>]
    let RemoveSubscription = "/010"

    [<Literal>]
    let SubscribeSearchAppointments = "/006"

    [<Literal>]
    let SubscribeSearchOthers = "/011"

    [<Literal>]
    let SubscribeSearchPassportResult = "/012"

    [<Literal>]
    let ChooseSubscriptionRequest = "/013"

    [<Literal>]
    let ChooseSubscriptionRequestWay = "/014"


type Name =
    | Start
    | Mine
    | Countries of embassyName: string
    | UserCountries of embassyName: string
    | Cities of embassyName: string * countryName: string
    | UserCities of embassyName: string * countryName: string
    | ChoseSubscriptionRequest of Embassy * command: string * way: string
    | ChoseSubscriptionRequestWay of Embassy * command: string
    | SubscriptionRequest of Embassy
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
    | Start -> Code.Start
    | Mine -> Code.Mine
    | Countries embassyName -> [ Code.Countries; embassyName ] |> build
    | Cities(embassyName, countryName) -> [ Code.Cities; embassyName; countryName ] |> build
    | UserCountries embassyName -> [ Code.UserCountries; embassyName ] |> build
    | UserCities(embassyName, countryName) -> [ Code.UserCities; embassyName; countryName ] |> build
    | UserSubscriptions embassy ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.UserSubscriptions; embassy ] |> build
    | ChoseSubscriptionRequestWay(embassy, command) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.ChooseSubscriptionRequestWay; embassy; command ] |> build
    | ChoseSubscriptionRequest(embassy, command, way) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.ChooseSubscriptionRequest; embassy; command; way ] |> build
    | SubscriptionRequest embassy ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SubscriptionRequest; embassy ] |> build
    | SubscribeSearchAppointments(embassy, payload) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SubscribeSearchAppointments; embassy; payload ] |> build
    | SubscribeSearchOthers(embassy, payload) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SubscribeSearchOthers; embassy; payload ] |> build
    | SubscribeSearchPassportResult(embassy, payload) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
        |> fun embassy -> [ Code.SubscribeSearchPassportResult; embassy; payload ] |> build
    | ChooseAppointmentRequest(embassy, appointmentId) ->
        embassy
        |> EA.Core.SerDe.Embassy.serialize
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
        | Code.ChooseSubscriptionRequestWay ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let command = parts[2]
                    Some(ChoseSubscriptionRequestWay(embassy, command)))
            | _ -> Ok <| None
        | Code.ChooseSubscriptionRequest ->
            match argsLength with
            | 3 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let command = parts[2]
                    let way = parts[3]
                    Some(ChoseSubscriptionRequest(embassy, command, way)))
            | _ -> Ok <| None
        | Code.SubscriptionRequest ->
            match argsLength with
            | 1 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (Some << SubscriptionRequest)
            | _ -> Ok <| None
        | Code.SubscribeSearchAppointments ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let payload = parts[2]
                    Some(SubscribeSearchAppointments(embassy, payload)))
            | _ -> Ok <| None
        | Code.SubscribeSearchOthers ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let payload = parts[2]
                    Some(SubscribeSearchOthers(embassy, payload)))
            | _ -> Ok <| None
        | Code.SubscribeSearchPassportResult ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (fun embassy ->
                    let payload = parts[2]
                    Some(SubscribeSearchPassportResult(embassy, payload)))
            | _ -> Ok <| None
        | Code.UserSubscriptions ->
            match argsLength with
            | 1 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
                |> Result.map (Some << UserSubscriptions)
            | _ -> Ok <| None
        | Code.ChooseAppointmentRequest ->
            match argsLength with
            | 2 ->
                parts[1]
                |> EA.Core.SerDe.Embassy.deserialize
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
