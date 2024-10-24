[<RequireQualifiedAccess>]
module EA.Telegram.Command

open EA.Domain
open Infrastructure.Logging

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

module private SerDe =
    open System.Text

    module Embassy =
        open EA.Mapper.Embassy

        let wrap (embassy: EA.Domain.Embassy) =
            let external = toExternal embassy
            let sb = StringBuilder()
            sb.Append external.Name |> ignore
            sb.Append '|' |> ignore
            sb.Append external.Country.Name |> ignore
            sb.Append '|' |> ignore
            sb.Append external.Country.City.Name |> ignore
            sb.ToString()

        let unwrap (value: string) =
            let parts = value.Split '|'
            let embassy = parts[0]
            let country = parts[1]
            let city = parts[2]

            (embassy, country, city)
            |> createInternal
            |> Result.defaultValue (Unchecked.defaultof<_>)

type Name =
    | Start
    | Mine
    | Countries of Embassy
    | UserCountries of Embassy
    | Cities of Embassy *Country
    | UserCities of Embassy* Country
    | SubscriptionRequest of Embassy * Country * City
    | Subscribe of Embassy
    | UserSubscriptions of Embassy * Country * City
    | ConfirmAppointment of Embassy * Appointment

let private build args = args |> String.concat "|"

let private printSize (value: string) =
    let size = System.Text.Encoding.UTF8.GetByteCount(value)
    $"'{value}' -> {size}" |> Log.info

let private create command =
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
    | ConfirmAppointment(embassy, appointment) ->

        [ List.ConfirmAppointment; embassy; country; city; payload ] |> build
    |> fun result ->
        result |> printSize
        result

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

module Create =
    open System
    open Infrastructure
    open Web.Telegram.Domain.Producer
    open EA.Telegram.Persistence

    let appointments (embassy, appointments: Set<Appointment>) =
        fun chatId ->
            { Buttons.Name = $"Appointments for {embassy}"
              Columns = 1
              Data =
                appointments
                |> Seq.map (fun x ->
                    (embassy, x) |> Name.ConfirmAppointment |> create,
                    x.Description |> Option.defaultValue "No description")
                |> Map }
            |> Response.Buttons.create (chatId, New)

    let confirmation (embassy, confirmations: Set<Confirmation>) =
        fun chatId ->
            confirmations
            |> Seq.map (fun confirmation -> $"'{embassy}'. Confirmation: {confirmation.Description}")
            |> String.concat "\n"
            |> Response.Text.create (chatId, New)

    let start chatId =
        EA.Api.getEmbassies ()
        |> Seq.map (fun embassy -> embassy |> Name.Countries |> create, embassy.Name)
        |> Seq.sortBy snd
        |> Map
        |> fun data ->
            { Buttons.Name = "Embassies"
              Columns = 3
              Data = data }
            |> Response.Buttons.create (chatId, New)
        |> Ok
        |> async.Return

    let mine chatId =
        fun cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.map (fun embassy -> embassy |> Name.UserCountries |> create, embassy.Name))
            |> ResultAsync.map (Seq.sortBy snd)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = "My Embassies"
                  Columns = 3
                  Data = data }
                |> Response.Buttons.create (chatId, New))

    let countries embassy =
        fun (chatId, msgId) ->
            let data =
                EA.Api.getEmbassyCountries embassy
                |> Seq.map (fun country -> country |> Name.Cities |> create, country.Name)
                |> Seq.sortBy snd
                |> Map

            { Buttons.Name = $"Available Countries"
              Columns = 3
              Data = data }
            |> Response.Buttons.create (chatId, msgId |> Replace)
            |> Ok
            |> async.Return

    let userCountries embassy =
        fun (chatId, msgId) cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.filter (fun x -> x = embassy))
            |> ResultAsync.map (Seq.map _.Country)
            |> ResultAsync.map (Seq.map (fun country -> country |> Name.UserCities |> create, country.Name))
            |> ResultAsync.map (Seq.sortBy snd)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Countries"
                  Columns = 3
                  Data = data }
                |> Response.Buttons.create (chatId, msgId |> Replace))

    let cities (embassy, country) =
        fun (chatId, msgId) ->
            let data =
                EA.Api.getEmbassyCountryCities embassy country
                |> Seq.map (fun city -> (embassy, country, city) |> Name.SubscriptionRequest |> create, city.Name)
                |> Seq.sortBy snd
                |> Map

            { Buttons.Name = $"Available Cities"
              Columns = 3
              Data = data }
            |> Response.Buttons.create (chatId, msgId |> Replace)
            |> Ok
            |> async.Return

    let userCities (embassy, country) =
        fun (chatId, msgId) cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.filter (fun x -> x = embassy && x.Country = country))
            |> ResultAsync.map (Seq.map _.Country.City)
            |> ResultAsync.map (
                Seq.map (fun city -> (embassy, country, city) |> Name.UserSubscriptions |> create, city.Name)
            )
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Cities"
                  Columns = 3
                  Data = data }
                |> Response.Buttons.create (chatId, msgId |> Replace))

    let subscriptionRequest embassy =
        fun (chatId, msgId) ->
            match embassy with
            | Russian _ ->
                let command = embassy |> Name.Subscribe |> create
                $"Send your payload using the following format: '{command}'."
            | _ -> $"Not supported embassy: '{embassy}'."
            |> Response.Text.create (chatId, msgId |> Replace)
            |> Ok
            |> async.Return

    let userSubscriptions embassy =
        fun (chatId, msgId) cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatRequests chat ct))
            |> ResultAsync.map (Seq.filter (fun request -> request.Embassy = embassy))
            |> ResultAsync.map (fun requests ->
                requests
                |> Seq.map (fun request -> $"{request.Id} -> {request.Payload}")
                |> String.concat Environment.NewLine
                |> (Response.Text.create (chatId, msgId |> Replace))))

    let subscribe (embassy, payload) =
        fun chatId cfg ct ->
            match embassy with
            | Russian _ ->

                let createOrUpdatePassportSearchRequest ct =
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (
                        Repository.Command.Request.createOrUpdatePassportSearch (embassy, payload) ct
                    )

                let createOrUpdateChatSubscription ct =
                    ResultAsync.bindAsync (fun (request: Request) ->
                        Storage.Chat.create cfg
                        |> ResultAsync.wrap (
                            Repository.Command.Chat.createOrUpdateSubscription (chatId, request.Id) ct
                        )
                        |> ResultAsync.map (fun _ -> request))

                createOrUpdatePassportSearchRequest ct
                |> createOrUpdateChatSubscription ct
                |> ResultAsync.map (fun request -> $"Subscription has been activated for '{request.Embassy}'.")
                |> ResultAsync.map (Response.Text.create (chatId, New))
            | _ -> embassy.Name |> NotSupported |> Error |> async.Return)

    let private confirmRussianAppointment request ct storage =
        let config: EA.Embassies.Russian.Domain.ProcessRequestConfiguration =
            { TimeShift = 0y }

        (storage, config, ct) |> EA.Deps.Russian.processRequest |> EA.Api.processRequest
        <| request

    let private handleRequest storage ct (request, appointment) =

        let request =
            { request with
                ConfirmationState = Manual appointment }

        match request.Embassy with
        | Russian _ -> storage |> confirmRussianAppointment request ct
        | _ -> "Embassy" |> NotSupported |> Error |> async.Return
        |> ResultAsync.map (fun request ->
            let confirmation =
                request.Appointments
                |> Seq.tryFind (fun x -> x.Value = appointment.Value)
                |> Option.map (fun appointment ->
                    appointment.Confirmation
                    |> Option.map _.Description
                    |> Option.defaultValue "Not found")

            $"'{request.Embassy}'. Confirmation: {confirmation}")

    let confirmAppointment (embassy, appointment) =
        fun chatId cfg ct ->
            Storage.Request.create cfg
            |> ResultAsync.wrap (fun storage ->
                storage
                |> Repository.Query.Chat.getChatEmbassyRequests chatId embassy ct
                |> ResultAsync.bindAsync (fun requests ->
                    match requests.Length with
                    | 0 -> "Request" |> NotFound |> Error |> async.Return
                    | 1 ->
                        (requests[0], appointment)
                        |> handleRequest storage ct
                        |> ResultAsync.map (Response.Text.create (chatId, New))))
