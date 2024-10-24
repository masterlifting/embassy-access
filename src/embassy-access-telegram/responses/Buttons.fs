[<RequireQualifiedAccess>]
module EA.Telegram.Responses.Buttons

open System
open Infrastructure
open Web.Telegram.Domain.Producer
open EA.Telegram.Persistence
open EA.Telegram.Responses
open EA.Domain

module Create =

    let appointments (embassy, appointments: Set<Appointment>) =
        fun chatId ->
            let embassy = embassy |> EA.Mapper.Embassy.toExternal

            { Buttons.Name = $"Appointments for {embassy}"
              Columns = 1
              Data =
                appointments
                |> Seq.map (fun x ->
                    (embassy.Name, embassy.Country.Name, embassy.Country.City.Name, x.Value)
                    |> EA.Telegram.Command.Name.ConfirmAppointment
                    |> EA.Telegram.Command.set,
                    x.Description |> Option.defaultValue "No description")
                |> Map }
            |> Response.createButtons (chatId, New)

    let embassies chatId =
        EA.Api.getEmbassies ()
        |> Seq.concat
        |> Seq.map EA.Mapper.Embassy.toExternal
        |> Seq.map (fun embassy ->
            embassy.Name |> EA.Telegram.Command.Name.Countries |> EA.Telegram.Command.set, embassy.Name)
        |> Seq.sortBy snd
        |> Map
        |> fun data ->
            { Buttons.Name = "Available Embassies"
              Columns = 3
              Data = data }
            |> Response.createButtons (chatId, New)
        |> Ok
        |> async.Return

    let embassies' chatId =
        EA.Api.getEmbassies ()
        |> Seq.concat
        |> Seq.map (fun embassy ->
            embassy |> EA.Telegram.Command.Name.Countries' |> EA.Telegram.Command.set, embassy |> string)
        |> Seq.sortBy snd
        |> Map
        |> fun data ->
            { Buttons.Name = "Available Embassies"
              Columns = 3
              Data = data }
            |> Response.createButtons (chatId, New)
        |> Ok
        |> async.Return

    let userEmbassies chatId =
        fun cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (
                Seq.map (fun embassy ->
                    embassy.Name
                    |> EA.Telegram.Command.Name.UserCountries
                    |> EA.Telegram.Command.set,
                    embassy.Name)
            )
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = "My Embassies"
                  Columns = 3
                  Data = data }
                |> Response.createButtons (chatId, New))

    let countries embassy' =
        fun (chatId, msgId) ->
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassy')
                |> Seq.map _.Country
                |> Seq.map (fun country ->
                    (embassy', country.Name)
                    |> EA.Telegram.Command.Name.Cities
                    |> EA.Telegram.Command.set,
                    country.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = $"Available Countries"
              Columns = 3
              Data = data }
            |> Response.createButtons (chatId, msgId |> Replace)
            |> Ok
            |> async.Return

    let userCountries embassy' =
        fun (chatId, msgId) cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (Seq.filter (fun embassy -> embassy.Name = embassy'))
            |> ResultAsync.map (Seq.map _.Country)
            |> ResultAsync.map (
                Seq.map (fun country ->
                    (embassy', country.Name)
                    |> EA.Telegram.Command.Name.UserCities
                    |> EA.Telegram.Command.set,
                    country.Name)
            )
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Countries"
                  Columns = 3
                  Data = data }
                |> Response.createButtons (chatId, msgId |> Replace))

    let cities (embassy', country') =
        fun (chatId, msgId) ->
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.filter (fun embassy -> embassy.Name = embassy')
                |> Seq.map _.Country
                |> Seq.filter (fun country -> country.Name = country')
                |> Seq.map _.City
                |> Seq.map (fun city ->
                    (embassy', country', city.Name)
                    |> EA.Telegram.Command.Name.SubscriptionRequest
                    |> EA.Telegram.Command.set,
                    city.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = $"Available Cities"
              Columns = 3
              Data = data }
            |> Response.createButtons (chatId, msgId |> Replace)
            |> Ok
            |> async.Return

    let userCities (embassy', country') =
        fun (chatId, msgId) cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (Seq.filter (fun country -> country.Name = embassy'))
            |> ResultAsync.map (Seq.map _.Country)
            |> ResultAsync.map (Seq.filter (fun city -> city.Name = country'))
            |> ResultAsync.map (Seq.map _.City)
            |> ResultAsync.map (
                Seq.map (fun city ->
                    (embassy', country', city.Name)
                    |> EA.Telegram.Command.Name.UserSubscriptions
                    |> EA.Telegram.Command.set,
                    city.Name)
            )
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Cities"
                  Columns = 3
                  Data = data }
                |> Response.createButtons (chatId, msgId |> Replace))
