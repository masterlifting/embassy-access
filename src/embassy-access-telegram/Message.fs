module EmbassyAccess.Telegram.Message

open System
open EmbassyAccess
open EmbassyAccess.Domain
open EmbassyAccess.Persistence
open Infrastructure
open Persistence.Domain
open Web.Telegram.Domain.Producer

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
            match (embassy', country', city') |> Mapper.Embassy.create with
            | Error error -> error.Message
            | Ok embassy ->
                match embassy with
                | Russian _ ->
                    $"Send your payload using the following format: '{embassy'}|{country'}|{city'}|your_link_here'."
                | _ -> $"Not supported embassy: '{embassy'}'."
            |> create (chatId, msgId |> Replace)
            |> Text

        let payloadResponse ct chatId (embassy', country', city') payload =
            (embassy', country', city')
            |> Mapper.Embassy.create
            |> ResultAsync.wrap (fun embassy ->
                match embassy with
                | Russian country ->
                    Storage.Context.InMemory
                    |> Persistence.Storage.create
                    |> ResultAsync.wrap (fun storage ->

                        let options: Command.Options.Request.PassportsGroup =
                            { Embassy = embassy
                              Payload = payload
                              ConfirmationState = Disabled
                              Validation = Some Api.validateRequest }

                        let operation =
                            options
                            |> Command.Options.Request.Create.PassportsGroup
                            |> Command.Request.Create

                        storage
                        |> Repository.Command.Request.execute ct operation
                        |> ResultAsync.bindAsync (fun request ->

                            let command =
                                (chatId, request.Id)
                                |> Persistence.Command.Options.Chat.ByRequestId
                                |> Persistence.Command.Chat.Create

                            storage
                            |> Persistence.Repository.Command.Chat.execute ct command
                            |> ResultAsync.map (fun _ -> request)))
                    |> ResultAsync.map (fun request -> $"Request created for '{request.Embassy}'.")
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

        let embassies chatId =
            let data =
                Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map Mapper.Embassy.toExternal
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
                Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map Mapper.Embassy.toExternal
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
                Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map Mapper.Embassy.toExternal
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
