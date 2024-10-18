[<RequireQualifiedAccess>]
module EA.Telegram.Message

open System
open Infrastructure
open Web.Telegram.Domain.Producer
open EA.Telegram.Persistence
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

        let payloadRequest (embassy', country', city') =
            fun (chatId, msgId) ->
                match (embassy', country', city') |> EA.Mapper.Embassy.createInternal with
                | Error error -> error.Message
                | Ok embassy ->
                    match embassy with
                    | Russian _ ->
                        $"Send your payload using the following format: '{embassy'}|{country'}|{city'}|your_link_here'."
                    | _ -> $"Not supported embassy: '{embassy'}'."
                |> create (chatId, msgId |> Replace)
                |> Text

        let userRequests (embassy', country', city') =
            fun (chatId, msgId) cfg ct ->
                EA.Mapper.Embassy.createInternal (embassy', country', city')
                |> ResultAsync.wrap (fun embassy ->
                    Storage.Chat.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.tryFind ct chatId)
                    |> ResultAsync.bindAsync (function
                        | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                        | Some chat ->
                            Storage.Request.create cfg
                            |> ResultAsync.wrap (Repository.Query.Request.getRequests ct chat))
                    |> ResultAsync.map (Seq.filter (fun request -> request.Embassy = embassy))
                    |> ResultAsync.map (fun requests ->
                        requests
                        |> Seq.map (fun request -> $"{request.Id} -> {request.Payload}")
                        |> String.concat Environment.NewLine
                        |> create (chatId, msgId |> Replace)
                        |> Text))

        let subscribe (embassy', country', city', payload) =
            fun chatId cfg ct ->
                (embassy', country', city')
                |> EA.Mapper.Embassy.createInternal
                |> ResultAsync.wrap (fun embassy ->
                    match embassy with
                    | Russian _ ->

                        let createOrUpdatePassportSearchRequest ct =
                            Storage.Request.create cfg
                            |> ResultAsync.wrap (
                                Repository.Command.Request.createOrUpdatePassportSearch ct (embassy, payload)
                            )

                        let createOrUpdateChatSubscription ct =
                            ResultAsync.bindAsync (fun (request: Request) ->
                                Storage.Chat.create cfg
                                |> ResultAsync.wrap (
                                    Repository.Command.Chat.createOrUpdateSubscription ct (chatId, request.Id)
                                )
                                |> ResultAsync.map (fun _ -> request))

                        createOrUpdatePassportSearchRequest ct
                        |> createOrUpdateChatSubscription ct
                        |> ResultAsync.map (fun request -> $"Subscription has been activated for '{request.Embassy}'.")
                        |> ResultAsync.map (create (chatId, New) >> Text)
                    | _ -> embassy' |> NotSupported |> Error |> async.Return)

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

        let embassies () =
            fun chatId ->
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

        let userEmbassies () =
            fun chatId cfg ct ->
                Storage.Chat.create cfg
                |> ResultAsync.wrap (Repository.Query.Chat.tryFind ct chatId)
                |> ResultAsync.bindAsync (function
                    | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                    | Some chat ->
                        Storage.Request.create cfg
                        |> ResultAsync.wrap (Repository.Query.Request.getEmbassies ct chat))
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

        let countries embassy' =
            fun (chatId, msgId) ->
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

        let userCountries embassy' (chatId, msgId) cfg ct =
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryFind ct chatId)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Request.getEmbassies ct chat))
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

        let cities (embassy', country') (chatId, msgId) =
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

        let userCities (embassy', country') (chatId, msgId) cfg ct =
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryFind ct chatId)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Request.getEmbassies ct chat))
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
