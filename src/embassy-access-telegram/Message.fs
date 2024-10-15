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
                |> Seq.map (fun embassy -> embassy.Name, embassy.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = "Available Embassies."
              Columns = 3
              Data = data }
            |> create (chatId, New)
            |> Buttons

        let countries (chatId, msgId) embassy' =
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassy')
                |> Seq.map _.Country
                |> Seq.map (fun country -> (embassy' + "|" + country.Name), country.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = $"Countries for '{embassy'}' embassy."
              Columns = 3
              Data = data }
            |> create (chatId, msgId |> Replace)
            |> Buttons

        let cities (chatId, msgId) (embassy', country') =
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassy')
                |> Seq.map _.Country
                |> Seq.filter (fun country -> country.Name = country')
                |> Seq.map _.City
                |> Seq.map (fun city -> (embassy' + "|" + country' + "|" + city.Name), city.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = $"Cities for '{embassy'}' embassy in '{country'}'."
              Columns = 3
              Data = data }
            |> create (chatId, msgId |> Replace)
            |> Buttons
