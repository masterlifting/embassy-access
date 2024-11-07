module EA.Telegram.CommandHandler

open System
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Domain
open EA.Telegram
open EA.Telegram.Persistence

let appointments (embassy, appointments: Set<Appointment>) =
    fun chatId ->
        { Buttons.Name = $"Appointments for {embassy}"
          Columns = 1
          Data =
            appointments
            |> Seq.map (fun appointment ->
                (embassy, appointment.Id) |> Command.ChooseAppointmentRequest |> Command.set, appointment.Description)
            |> Map }
        |> Web.Telegram.Producer.Buttons.create (chatId, New)

let confirmation (embassy, confirmations: Set<Confirmation>) =
    fun chatId ->
        confirmations
        |> Seq.map (fun confirmation -> $"'{embassy}'. Confirmation: {confirmation.Description}")
        |> String.concat "\n"
        |> Web.Telegram.Producer.Text.create (chatId, New)

let start chatId =
    EA.Api.getEmbassies ()
    |> Seq.map EA.Mapper.Embassy.toExternal
    |> Seq.groupBy _.Name
    |> Seq.map fst
    |> Seq.sort
    |> Seq.map (fun embassyName -> embassyName |> Command.Countries |> Command.set, embassyName)
    |> Map
    |> fun data ->
        { Buttons.Name = "Choose embassy to get a service"
          Columns = 3
          Data = data }
        |> Buttons.create (chatId, New)
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
            |> Seq.map (fun countryName -> (embassyName, countryName) |> Command.Cities |> Command.set, countryName)
            |> Map

        { Buttons.Name = $"Where is {embassyName} embassy located?"
          Columns = 3
          Data = data }
        |> Buttons.create (chatId, msgId |> Replace)
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
            |> Result.map (fun x -> x |> Command.SubscriptionRequest |> Command.set, embassy.Country.City.Name))
        |> Result.choose
        |> Result.map Map
        |> Result.map (fun data ->
            { Buttons.Name = $"Which city in {countryName}?"
              Columns = 3
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))
        |> async.Return

let mine chatId =
    fun cfg ct ->
        Storage.FileSystem.Chat.create cfg
        |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
        |> ResultAsync.bindAsync (function
            | None -> "Subscriptions" |> NotFound |> Error |> async.Return
            | Some chat ->
                EA.Persistence.Storage.FileSystem.Request.create cfg
                |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
        |> ResultAsync.map (fun embassies ->
            embassies
            |> Seq.map EA.Mapper.Embassy.toExternal
            |> Seq.groupBy _.Name
            |> Seq.map fst
            |> Seq.sort
            |> Seq.map (fun embassyName -> embassyName |> Command.UserCountries |> Command.set, embassyName)
            |> Map)
        |> ResultAsync.map (fun data ->
            { Buttons.Name = "Choose embassy to look at your services"
              Columns = 3
              Data = data }
            |> Buttons.create (chatId, New))

let userCountries embassyName =
    fun (chatId, msgId) cfg ct ->
        Storage.FileSystem.Chat.create cfg
        |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
        |> ResultAsync.bindAsync (function
            | None -> "Subscriptions" |> NotFound |> Error |> async.Return
            | Some chat ->
                EA.Persistence.Storage.FileSystem.Request.create cfg
                |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
        |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
        |> ResultAsync.map (fun embassies ->
            embassies
            |> Seq.filter (fun embassy -> embassy.Name = embassyName)
            |> Seq.groupBy _.Country.Name
            |> Seq.map fst
            |> Seq.sort
            |> Seq.map (fun countryName ->
                (embassyName, countryName) |> Command.UserCities |> Command.set, countryName)
            |> Map)
        |> ResultAsync.map (fun data ->
            { Buttons.Name = $"Where is {embassyName} embassy located?"
              Columns = 3
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))

let userCities (embassyName, countryName) =
    fun (chatId, msgId) cfg ct ->
        Storage.FileSystem.Chat.create cfg
        |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
        |> ResultAsync.bindAsync (function
            | None -> "Subscriptions" |> NotFound |> Error |> async.Return
            | Some chat ->
                EA.Persistence.Storage.FileSystem.Request.create cfg
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
                |> Result.map (fun x -> x |> Command.UserSubscriptions |> Command.set, embassy.Country.City.Name))
            |> Result.choose
            |> Result.map Map)
        |> ResultAsync.map (fun data ->
            { Buttons.Name = $"Which city in {countryName}?"
              Columns = 3
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))

let subscriptionRequest embassy =
    fun (chatId, msgId) ->
        match embassy with
        | Russian _ ->
            [ "searchappointments", "Check passport appointments"
              "searchothers", "Check other appointments"
              "searchpassportresult", "Check passport readiness" ]
            |> Seq.map (fun (key, name) -> (embassy, key) |> Command.ChoseSubscriptionRequestWay |> Command.set, name)
            |> Map
            |> Ok
        | _ -> $"{embassy}" |> NotSupported |> Error
        |> Result.map (fun data ->
            { Buttons.Name = "Which service do you want to use?"
              Columns = 1
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))
        |> async.Return

let chooseSubscriptionRequestWay (embassy, command) =
    fun (chatId, msgId) ->
        match embassy with
        | Russian _ ->
            [ "now", "Immediately"; "background", "In background" ]
            |> Seq.map (fun (key, name) ->
                (embassy, command, key) |> Command.ChoseSubscriptionRequest |> Command.set, name)
            |> Map
            |> Ok
        | _ -> $"{embassy}" |> NotSupported |> Error
        |> Result.map (fun data ->
            { Buttons.Name = "Which way do you want to use?"
              Columns = 1
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))
        |> async.Return

let choseSubscriptionRequest (embassy, command, way) =
    fun (chatId, msgId) ->
        match embassy with
        | Russian _ ->
            match way with
            | "background" ->
                let command =
                    match command with
                    | "searchappointments" -> (embassy, "your_link_here") |> Command.SubscribeSearchAppointments |> Ok
                    | "searchothers" -> (embassy, "your_link_here") |> Command.SubscribeSearchOthers |> Ok
                    | "searchpassportresult" -> command |> NotSupported |> Error
                    | _ -> command |> NotSupported |> Error

                command
                |> Result.map Command.set
                |> Result.map (fun cmd -> $"Send your payload using the following format: '{cmd}'.")
            | _ -> $"{way}" |> NotSupported |> Error
        | _ -> $"{embassy}" |> NotSupported |> Error
        |> Result.map (Text.create (chatId, Replace msgId))
        |> async.Return

let userSubscriptions embassy =
    fun (chatId, msgId) cfg ct ->
        Storage.FileSystem.Chat.create cfg
        |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
        |> ResultAsync.bindAsync (function
            | None -> "Subscriptions" |> NotFound |> Error |> async.Return
            | Some chat ->
                EA.Persistence.Storage.FileSystem.Request.create cfg
                |> ResultAsync.wrap (Repository.Query.Chat.getChatRequests chat ct))
        |> ResultAsync.map (Seq.filter (fun request -> request.Embassy = embassy))
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun request -> request.Id |> Command.RemoveSubscription |> Command.set, request.Payload)
            |> Map)
        |> ResultAsync.map (fun data ->
            { Buttons.Name = "You have the following subscriptions to remove"
              Columns = 1
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))

let subscribe (embassy, payload, name) =
    fun chatId cfg ct ->
        match embassy with
        | Russian _ ->

            let createOrUpdatePassportSearchRequest ct =
                EA.Persistence.Storage.FileSystem.Request.create cfg
                |> ResultAsync.wrap (fun storage ->
                    let command =
                        match name with
                        | "searchappointments" ->
                            Repository.Command.Request.createOrUpdatePassportSearch (embassy, payload) ct
                        | "searchothers" -> Repository.Command.Request.createOrUpdateOthersSearch (embassy, payload) ct
                        | "searchpassportresult" -> fun _ -> name |> NotSupported |> Error |> async.Return
                        | _ -> fun _ -> name |> NotSupported |> Error |> async.Return

                    storage |> command)

            let createOrUpdateChatSubscription ct =
                ResultAsync.bindAsync (fun (request: Request) ->
                    Storage.FileSystem.Chat.create cfg
                    |> ResultAsync.wrap (Repository.Command.Chat.createOrUpdateSubscription (chatId, request.Id) ct)
                    |> ResultAsync.map (fun _ -> request))

            createOrUpdatePassportSearchRequest ct
            |> createOrUpdateChatSubscription ct
            |> ResultAsync.map (fun request -> $"Subscription has been activated for '{request.Embassy}'.")
            |> ResultAsync.map (Text.create (chatId, New))
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

let confirmAppointment (requestId, appointmentId) =
    fun chatId cfg ct ->
        EA.Persistence.Storage.FileSystem.Request.create cfg
        |> ResultAsync.wrap (fun storage ->
            let query = EA.Persistence.Query.Request.GetOne.Id requestId

            storage
            |> EA.Persistence.Repository.Query.Request.getOne query ct
            |> ResultAsync.bindAsync (function
                | None -> "Request" |> NotFound |> Error |> async.Return
                | Some request ->
                    (request, appointmentId)
                    |> handleRequest storage ct
                    |> ResultAsync.map (Text.create (chatId, New))))

let chooseAppointmentRequest (embassy, appointmentId) =
    fun chatId cfg ct ->
        EA.Persistence.Storage.FileSystem.Request.create cfg
        |> ResultAsync.wrap (fun storage ->
            storage
            |> Repository.Query.Chat.getChatEmbassyRequests chatId embassy ct
            |> ResultAsync.bindAsync (fun requests ->
                match requests.Length with
                | 0 -> "Request" |> NotFound |> Error |> async.Return
                | 1 -> confirmAppointment (requests[0].Id, appointmentId) chatId cfg ct
                | _ ->
                    requests
                    |> Seq.map (fun request ->
                        (request.Id, appointmentId) |> Command.ConfirmAppointment |> Command.set, request.Payload)
                    |> Map
                    |> (fun data ->
                        { Buttons.Name = $"Choose the Request"
                          Columns = 1
                          Data = data }
                        |> Buttons.create (chatId, New))
                    |> Ok
                    |> async.Return))

let removeSubscription subscriptionId =
    fun chatId cfg ct ->
        EA.Persistence.Storage.FileSystem.Request.create cfg
        |> ResultAsync.wrap (fun storage ->
            let command =
                subscriptionId
                |> EA.Persistence.Command.Definitions.Request.Delete.RequestId
                |> EA.Persistence.Command.Request.Delete

            storage
            |> EA.Persistence.Repository.Command.Request.execute command ct
            |> ResultAsync.bindAsync (fun _ ->
                Storage.FileSystem.Chat.create cfg
                |> ResultAsync.wrap (fun storage ->
                    let command =
                        (chatId, subscriptionId)
                        |> Command.Definitions.Chat.Delete.Subscription
                        |> Command.Chat.Delete

                    storage
                    |> Repository.Command.Chat.execute command ct
                    |> ResultAsync.map (fun _ -> $"{subscriptionId} has been removed."))))
        |> ResultAsync.map (Text.create (chatId, New))
