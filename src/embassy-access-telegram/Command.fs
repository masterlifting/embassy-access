[<RequireQualifiedAccess>]
module EA.Telegram.Command

open System
open EA.Domain
open Infrastructure.Logging

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

module private SerDe =
    open System.Text

    module Embassy =
        open EA.Mapper.Embassy

        let wrap (embassy: Embassy) =
            match embassy with
            | Russian embassy ->
                match embassy.Country with
                | Serbia city ->
                    match city with
                    | Belgrade -> "001"
                | Albania city ->
                    match city with
                    | Tirana -> "014"
            | _ -> "000"
            

        let unwrap (value: string) =
            let parts = value.Split ','
            let embassy = parts[0]
            let country = parts[1]
            let city = parts[2]
            let guid = parts[3] |> Guid.Parse


            (embassy, country, city)
            |> createInternal
            |> Result.defaultValue (Unchecked.defaultof<_>)

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
    | UserSubscriptions embassy ->
        let embassy = embassy |> SerDe.Embassy.wrap
        [ List.UserSubscriptions; embassy ] |> build
    | SubscriptionRequest embassy ->
        let embassy = embassy |> SerDe.Embassy.wrap
        [ List.SubscriptionRequest; embassy ] |> build
    | Subscribe(embassy, payload) ->
        let embassy = embassy |> SerDe.Embassy.wrap
        [ List.Subscribe; embassy; payload ] |> build
    | ConfirmAppointment(embassy, appointmentId) ->
        let embassy = embassy |> SerDe.Embassy.wrap
        let appointmentId = appointmentId.Value |> string
        [ List.ConfirmAppointment; embassy; appointmentId ] |> build
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
            | 1 ->
                let embassy = parts[1] |> SerDe.Embassy.unwrap
                Some(SubscriptionRequest(embassy))
            | _ -> None
        | List.Subscribe ->
            match argsLength with
            | 2 ->
                let embassy = parts[1] |> SerDe.Embassy.unwrap
                let payload = parts[2]
                Some(Subscribe(embassy, payload))
            | _ -> None
        | List.UserSubscriptions ->
            match argsLength with
            | 1 ->
                let embassy = parts[1] |> SerDe.Embassy.unwrap
                Some(UserSubscriptions(embassy))
            | _ -> None
        | List.ConfirmAppointment ->
            match argsLength with
            | 2 ->
                let embassy = parts[1] |> SerDe.Embassy.unwrap
                let appointmentId = parts[2] |> Guid.Parse |> AppointmentId
                Some(ConfirmAppointment(embassy, appointmentId))
            | _ -> None
        | _ -> None

module Create =
    open Infrastructure
    open Web.Telegram.Domain.Producer
    open EA.Telegram.Persistence

    let appointments (embassy, appointments: Set<Appointment>) =
        fun chatId ->
            { Buttons.Name = $"Appointments for {embassy}"
              Columns = 1
              Data =
                appointments
                |> Seq.map (fun appointment ->
                    (embassy, appointment.Id) |> Name.ConfirmAppointment |> create,
                    appointment.Description |> Option.defaultValue "No description")
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
        |> Seq.map EA.Mapper.Embassy.toExternal
        |> Seq.groupBy _.Name
        |> Seq.map fst
        |> Seq.sort
        |> Seq.map (fun embassyName -> embassyName |> Name.Countries |> create, embassyName)
        |> Map
        |> fun data ->
            { Buttons.Name = "Embassies"
              Columns = 3
              Data = data }
            |> Response.Buttons.create (chatId, New)
        |> Ok
        |> async.Return

    let countries embassyName =
        fun (chatId, msgId) ->
            let data =
                EA.Api.getEmbassies ()
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassyName)
                |> Seq.groupBy _.Country.Name
                |> Seq.map fst
                |> Seq.sort
                |> Seq.map (fun countryName -> (embassyName, countryName) |> Name.Cities |> create, countryName)
                |> Map

            { Buttons.Name = $"Countries"
              Columns = 3
              Data = data }
            |> Response.Buttons.create (chatId, msgId |> Replace)
            |> Ok
            |> async.Return

    let cities (embassyName, countryName) =
        fun (chatId, msgId) ->
            EA.Api.getEmbassies ()
            |> Seq.map EA.Mapper.Embassy.toExternal
            |> Seq.filter (fun embassy -> embassy.Name = embassyName && embassy.Country.Name = countryName)
            |> Seq.groupBy _.Country.City.Name
            |> Seq.sortBy fst
            |> Seq.collect (fun (_, embassies) -> embassies |> Seq.take 1)
            |> Seq.map (fun embassy ->
                embassy
                |> EA.Mapper.Embassy.toInternal
                |> Result.map (fun x -> x |> Name.SubscriptionRequest |> create, embassy.Country.City.Name))
            |> Result.choose
            |> Result.map Map
            |> Result.map (fun data ->
                { Buttons.Name = $"Cities"
                  Columns = 3
                  Data = data }
                |> Response.Buttons.create (chatId, msgId |> Replace))
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
            |> ResultAsync.map (fun embassies ->
                embassies
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.groupBy _.Name
                |> Seq.map fst
                |> Seq.sort
                |> Seq.map (fun embassyName -> embassyName |> Name.UserCountries |> create, embassyName)
                |> Map)
            |> ResultAsync.map (fun data ->
                { Buttons.Name = "My Embassies"
                  Columns = 3
                  Data = data }
                |> Response.Buttons.create (chatId, New))

    let userCountries embassyName =
        fun (chatId, msgId) cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (fun embassies ->
                embassies
                |> Seq.filter (fun embassy -> embassy.Name = embassyName)
                |> Seq.groupBy _.Country.Name
                |> Seq.map fst
                |> Seq.sort
                |> Seq.map (fun countryName -> (embassyName, countryName) |> Name.UserCities |> create, countryName)
                |> Map)
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Countries"
                  Columns = 3
                  Data = data }
                |> Response.Buttons.create (chatId, msgId |> Replace))

    let userCities (embassyName, countryName) =
        fun (chatId, msgId) cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.bind (fun embassies ->
                embassies
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassyName && embassy.Country.Name = countryName)
                |> Seq.groupBy _.Country.City.Name
                |> Seq.sortBy fst
                |> Seq.collect (fun (_, embassies) -> embassies |> Seq.take 1)
                |> Seq.map (fun embassy ->
                    embassy
                    |> EA.Mapper.Embassy.toInternal
                    |> Result.map (fun x -> x |> Name.UserSubscriptions |> create, embassy.Country.City.Name))
                |> Result.choose
                |> Result.map Map)
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Cities"
                  Columns = 3
                  Data = data }
                |> Response.Buttons.create (chatId, msgId |> Replace))

    let subscriptionRequest embassy =
        fun (chatId, msgId) ->
            match embassy with
            | Russian _ ->
                let command = (embassy, "your_link_here") |> Name.Subscribe |> create
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
                |> (Response.Text.create (chatId, msgId |> Replace)))

    let subscribe (embassy, payload) =
        fun chatId cfg ct ->
            match embassy with
            | Russian _ ->

                let createOrUpdatePassportSearchRequest ct =
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Command.Request.createOrUpdatePassportSearch (embassy, payload) ct)

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
            | _ -> $"{embassy}" |> NotSupported |> Error |> async.Return

    let private confirmRussianAppointment request ct storage =
        let config: EA.Embassies.Russian.Domain.ProcessRequestConfiguration =
            { TimeShift = 0y }

        (storage, config, ct) |> EA.Deps.Russian.processRequest |> EA.Api.processRequest
        <| request

    let private handleRequest storage ct (request, appointmentId) =

        let request =
            { request with
                ConfirmationState = Manual appointmentId }

        match request.Embassy with
        | Russian _ -> storage |> confirmRussianAppointment request ct
        | _ -> "Embassy" |> NotSupported |> Error |> async.Return
        |> ResultAsync.map (fun request ->
            let confirmation =
                request.Appointments
                |> Seq.tryFind (fun x -> x.Id = appointmentId)
                |> Option.map (fun appointment ->
                    appointment.Confirmation
                    |> Option.map _.Description
                    |> Option.defaultValue "Not found")

            $"'{request.Embassy}'. Confirmation: {confirmation}")

    let confirmAppointment (embassy, appointmentId) =
        fun chatId cfg ct ->
            Storage.Request.create cfg
            |> ResultAsync.wrap (fun storage ->
                storage
                |> Repository.Query.Chat.getChatEmbassyRequests chatId embassy ct
                |> ResultAsync.bindAsync (fun requests ->
                    match requests.Length with
                    | 0 -> "Request" |> NotFound |> Error |> async.Return
                    | 1 ->
                        (requests[0], appointmentId)
                        |> handleRequest storage ct
                        |> ResultAsync.map (Response.Text.create (chatId, New))))
