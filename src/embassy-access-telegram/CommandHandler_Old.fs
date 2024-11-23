module EA.Telegram.CommandHandler_Old

open System

open EA.Embassies.Russian.Kdmid.Domain
open Infrastructure
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer
open EA.Core.Domain
open EA.Telegram
open EA.Telegram.Persistence

let appointments (embassy, appointments: Set<Appointment>) =
    fun chatId ->
        { Buttons.Name = $"Choose the appointment for '{embassy}'"
          Columns = 1
          Data =
            appointments
            |> Seq.map (fun appointment ->
                (embassy, appointment.Id) |> Command.ChooseAppointmentRequest |> Command.set, appointment.Description)
            |> Map }
        |> Buttons.create (chatId, New)

let confirmation (embassy, confirmations: Set<Confirmation>) =
    fun chatId ->
        confirmations
        |> Seq.map (fun confirmation -> $"'{embassy}'. Confirmation: {confirmation.Description}")
        |> String.concat "\n"
        |> Text.create (chatId, New)

let subscriptionRequest embassy =
    fun (chatId, msgId) ->
        match embassy with
        | Russian _ ->
            [ "searchappointments", "Search passport appointments"
              "searchothers", "Search others appointments"
              "searchpassportresult", "Check passport readiness" ]
            |> Seq.map (fun (key, name) -> (embassy, key) |> Command.ChoseSubscriptionRequestWay |> Command.set, name)
            |> Map
            |> Ok
        | _ -> $"{embassy}" |> NotSupported |> Error
        |> Result.map (fun data ->
            { Buttons.Name = "Which service would you like to get?"
              Columns = 1
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))
        |> async.Return

let chooseSubscriptionRequestWay (embassy, command) =
    fun (chatId, msgId) ->
        match embassy with
        | Russian _ ->
            [ "now", "Immediately"; "background", "When it will be available" ]
            |> Seq.map (fun (key, name) ->
                (embassy, command, key) |> Command.ChoseSubscriptionRequest |> Command.set, name)
            |> Map
            |> Ok
        | _ -> $"{embassy}" |> NotSupported |> Error
        |> Result.map (fun data ->
            { Buttons.Name = "When do you need the result?"
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
                |> Result.map (fun cmd ->
                    $"To obtain the result for this service, please send back the command along with the link where you need to check the result.{Environment.NewLine}The command is: {cmd}.")
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
        |> ResultAsync.map (Seq.filter (fun request -> request.Service.Embassy = embassy))
        |> ResultAsync.map (fun requests ->
            requests
            |> Seq.map (fun request ->
                request.Id |> Command.RemoveSubscription |> Command.set, request.Service.Payload)
            |> Map)
        |> ResultAsync.map (fun data ->
            { Buttons.Name =
                $"Your subscriptions for{Environment.NewLine}{embassy}{Environment.NewLine}To remove a subscription, click on it."
              Columns = 1
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))

// let subscribe (embassy, payload, service) =
//     fun chatId cfg ct ->
//         match embassy with
//         | Russian _ ->
//
//             let createOrUpdatePassportSearchRequest ct =
//                 EA.Persistence.Storage.FileSystem.Request.create cfg
//                 |> ResultAsync.wrap (fun storage ->
//                     let command =
//                         match service with
//                         | "searchappointments" ->
//                             Repository.Command.Request.createOrUpdatePassportSearch (embassy, payload) ct
//                         | "searchothers" -> Repository.Command.Request.createOrUpdateOthersSearch (embassy, payload) ct
//                         | "searchpassportresult" -> fun _ -> service |> NotSupported |> Error |> async.Return
//                         | _ -> fun _ -> service |> NotSupported |> Error |> async.Return
//
//                     storage |> command)
//
//             let createOrUpdateChatSubscription ct =
//                 ResultAsync.bindAsync (fun (request: Request) ->
//                     Storage.FileSystem.Chat.create cfg
//                     |> ResultAsync.wrap (Repository.Command.Chat.createOrUpdateSubscription (chatId, request.Id) ct)
//                     |> ResultAsync.map (fun _ -> request))
//
//             createOrUpdatePassportSearchRequest ct
//             |> createOrUpdateChatSubscription ct
//             |> ResultAsync.map (fun request -> $"Subscription has been activated for '{request.Service.Embassy}'.")
//             |> ResultAsync.map (Text.create (chatId, New))
//         | _ -> $"{embassy}" |> NotSupported |> Error |> async.Return

let private confirmRussianAppointment request ct storage =
    let deps = Dependencies.create ct storage
    let timeZone = 1 |> float
    let order = StartOrder.create timeZone request
    order |> EA.Embassies.Russian.API.Order.Kdmid.start deps

let private handleRequest storage ct (request, appointmentId) =

    let request =
        { request with
            ConfirmationState = Manual appointmentId }

    match request.Service.Embassy with
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

        $"'{request.Service.Embassy}'. Confirmation: {confirmation}")

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
                        (request.Id, appointmentId) |> Command.ConfirmAppointment |> Command.set,
                        request.Service.Payload)
                    |> Map
                    |> (fun data ->
                        { Buttons.Name = $"Which subscription do you want to confirm?"
                          Columns = 1
                          Data = data }
                        |> Buttons.create (chatId, New))
                    |> Ok
                    |> async.Return))

let removeSubscription subscriptionId =
    fun chatId cfg ct ->
        EA.Persistence.Storage.FileSystem.Request.create cfg
        |> ResultAsync.wrap (fun storage ->
            let command = subscriptionId |> EA.Persistence.Command.Request.Delete

            command
            |> EA.Persistence.Repository.Command.Request.execute storage ct
            |> ResultAsync.bindAsync (fun _ ->
                Storage.FileSystem.Chat.create cfg
                |> ResultAsync.wrap (fun storage ->
                    let command = chatId |> Command.Chat.Delete

                    storage
                    |> Repository.Command.Chat.execute command ct
                    |> ResultAsync.map (fun _ -> $"{subscriptionId} has been removed."))))
        |> ResultAsync.map (Text.create (chatId, New))
