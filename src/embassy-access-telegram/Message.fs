[<RequireQualifiedAccess>]
module EA.Telegram.Message

open System
open Infrastructure
open Web.Telegram.Domain.Producer
open EA.Telegram.Domain.Message
open EA.Domain

let create<'a> (chatId, msgId) (value: 'a) =
    { Id = msgId
      ChatId = chatId
      Value = value }

module Create =

    module Text =

        let error (error: Error') =
            fun chatId -> error.Message |> create (chatId, New) |> Text

        let confirmation (embassy, (confirmations: Set<Confirmation>)) =
            fun chatId ->
                confirmations
                |> Seq.map _.Description
                |> String.concat "\n"
                |> create (chatId, New)
                |> Text

        let payloadRequest (chatId, msgId) (embassy', country', city') =
            match (embassy', country', city') |> EA.Mapper.Embassy.createInternal with
            | Error error -> error.Message
            | Ok embassy ->
                match embassy with
                | Russian _ ->
                    $"Send your payload using the following format: '{embassy'}|{country'}|{city'}|your_link_here'."
                | _ -> $"Not supported embassy: '{embassy'}'."
            |> create (chatId, msgId |> Replace)
            |> Text

        let listRequests ct cfg (chatId, msgId) (embassy', country', city') =
            EA.Mapper.Embassy.createInternal (embassy', country', city')
            |> ResultAsync.wrap (fun embassy ->
                Persistence.Storage.Chat.create cfg
                |> ResultAsync.wrap (Persistence.Repository.Query.Chat.tryFind ct chatId)
                |> ResultAsync.bindAsync (function
                    | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                    | Some chat ->
                        Persistence.Storage.Request.create cfg
                        |> ResultAsync.wrap (Persistence.Repository.Query.Request.getRequests ct chat))
                |> ResultAsync.map (Seq.filter (fun request -> request.Embassy = embassy))
                |> ResultAsync.map (fun requests ->
                    requests
                    |> Seq.map (fun request -> $"{request.Id} -> {request.Payload}")
                    |> String.concat Environment.NewLine
                    |> create (chatId, msgId |> Replace)
                    |> Text))

        let payloadResponse (data: PayloadResponse) =
            (data.Embassy, data.Country, data.City)
            |> EA.Mapper.Embassy.createInternal
            |> ResultAsync.wrap (fun embassy ->
                match embassy with
                | Russian _ ->

                    let createOrUpdatePassportSearchRequest ct =
                        Persistence.Storage.Request.create data.Config
                        |> ResultAsync.wrap (
                            Persistence.Repository.Command.Request.createOrUpdatePassportSearch
                                ct
                                (embassy, data.Payload)
                        )

                    let createOrUpdateChatSubscription ct =
                        ResultAsync.bindAsync (fun (request: Request) ->
                            Persistence.Storage.Chat.create data.Config
                            |> ResultAsync.wrap (
                                Persistence.Repository.Command.Chat.createOrUpdateSubscription
                                    ct
                                    (data.ChatId, request.Id)
                            )
                            |> ResultAsync.map (fun _ -> request))

                    createOrUpdatePassportSearchRequest data.Ct
                    |> createOrUpdateChatSubscription data.Ct
                    |> ResultAsync.map (fun request -> $"Subscription has been activated for '{request.Embassy}'.")
                    |> ResultAsync.map (create (data.ChatId, New) >> Text)
                | _ -> data.Embassy |> NotSupported |> Error |> async.Return)

    module Buttons =

        let appointments (embassy, appointments: Set<Appointment>) =
            fun chatId ->
                { Buttons.Name = $"Found Appointments for {embassy}"
                  Columns = 3
                  Data =
                    appointments
                    |> Seq.map (fun x -> x.Value, x.Description |> Option.defaultValue "No data")
                    |> Map.ofSeq }
                |> create (chatId, New)
                |> Buttons

        let embassies chatId =
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.map (fun embassy -> "strt$" + embassy.Name, embassy.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = "Available Embassies"
              Columns = 3
              Data = data }
            |> create (chatId, New)
            |> Buttons

        let chatEmbassies ct cfg chatId =
            Persistence.Storage.Chat.create cfg
            |> ResultAsync.wrap (Persistence.Repository.Query.Chat.tryFind ct chatId)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Persistence.Storage.Request.create cfg
                    |> ResultAsync.wrap (Persistence.Repository.Query.Request.getEmbassies ct chat))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (Seq.map (fun embassy -> "mine$" + embassy.Name, embassy.Name))
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = "My Embassies"
                  Columns = 3
                  Data = data }
                |> create (chatId, New)
                |> Buttons)

        let countries (chatId, msgId) embassy' =
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassy')
                |> Seq.map _.Country
                |> Seq.map (fun country -> "strt$" + embassy' + "|" + country.Name, country.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = $"Available Countries"
              Columns = 3
              Data = data }
            |> create (chatId, msgId |> Replace)
            |> Buttons

        let chatCountries ct cfg (chatId, msgId) embassy' =
            Persistence.Storage.Chat.create cfg
            |> ResultAsync.wrap (Persistence.Repository.Query.Chat.tryFind ct chatId)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Persistence.Storage.Request.create cfg
                    |> ResultAsync.wrap (Persistence.Repository.Query.Request.getEmbassies ct chat))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (Seq.filter (fun embassy -> embassy.Name = embassy'))
            |> ResultAsync.map (Seq.map _.Country)
            |> ResultAsync.map (Seq.map (fun country -> "mine$" + embassy' + "|" + country.Name, country.Name))
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Countries"
                  Columns = 3
                  Data = data }
                |> create (chatId, msgId |> Replace)
                |> Buttons)

        let cities (chatId, msgId) (embassy', country') =
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassy')
                |> Seq.map _.Country
                |> Seq.filter (fun country -> country.Name = country')
                |> Seq.map _.City
                |> Seq.map (fun city -> "strt$" + embassy' + "|" + country' + "|" + city.Name, city.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = $"Available Cities"
              Columns = 3
              Data = data }
            |> create (chatId, msgId |> Replace)
            |> Buttons

        let chatCities ct cfg (chatId, msgId) (embassy', country') =
            Persistence.Storage.Chat.create cfg
            |> ResultAsync.wrap (Persistence.Repository.Query.Chat.tryFind ct chatId)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Persistence.Storage.Request.create cfg
                    |> ResultAsync.wrap (Persistence.Repository.Query.Request.getEmbassies ct chat))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (Seq.filter (fun country -> country.Name = embassy'))
            |> ResultAsync.map (Seq.map _.Country)
            |> ResultAsync.map (Seq.filter (fun city -> city.Name = country'))
            |> ResultAsync.map (Seq.map _.City)
            |> ResultAsync.map (Seq.map (fun city -> "mine$" + embassy' + "|" + country' + "|" + city.Name, city.Name))
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Cities"
                  Columns = 3
                  Data = data }
                |> create (chatId, msgId |> Replace)
                |> Buttons)
