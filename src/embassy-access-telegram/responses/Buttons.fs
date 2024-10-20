[<RequireQualifiedAccess>]
module EA.Telegram.Responses.Buttons

open System
open Infrastructure
open Web.Telegram.Domain.Producer
open EA.Telegram.Persistence
open EA.Telegram.Responses
open EA.Telegram.Domain
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
                    [ Key.APT
                      embassy.Name
                      embassy.Country.Name
                      embassy.Country.City.Name
                      x.Value ]
                    |> Key.wrap,
                    x.Description |> Option.defaultValue "No description")
                |> Map }
            |> Response.createButtons (chatId, New)

    let embassies () =
        fun chatId ->
            let data =
                EA.Api.getEmbassies ()
                |> Seq.concat
                |> Seq.map EA.Mapper.Embassy.toExternal
                |> Seq.map (fun embassy -> [ Key.SUB; embassy.Name ] |> Key.wrap, embassy.Name)
                |> Seq.sortBy fst
                |> Map

            { Buttons.Name = "Available Embassies"
              Columns = 3
              Data = data }
            |> Response.createButtons (chatId, New)
            |> Ok
            |> async.Return

    let userEmbassies () =
        fun chatId cfg ct ->
            Storage.Chat.create cfg
            |> ResultAsync.wrap (Repository.Query.Chat.tryGetOne chatId ct)
            |> ResultAsync.bindAsync (function
                | None -> "Subscriptions" |> NotFound |> Error |> async.Return
                | Some chat ->
                    Storage.Request.create cfg
                    |> ResultAsync.wrap (Repository.Query.Chat.getChatEmbassies chat ct))
            |> ResultAsync.map (Seq.map EA.Mapper.Embassy.toExternal)
            |> ResultAsync.map (Seq.map (fun embassy -> [ Key.INF; embassy.Name ] |> Key.wrap, embassy.Name))
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
                |> Seq.map (fun country -> [ Key.SUB; embassy'; country.Name ] |> Key.wrap, country.Name)
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
            |> ResultAsync.map (Seq.map (fun country -> [ Key.INF; embassy'; country.Name ] |> Key.wrap, country.Name))
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
                |> Seq.map (fun city -> [ Key.SUB; embassy'; country'; city.Name ] |> Key.wrap, city.Name)
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
            |> ResultAsync.map (Seq.map (fun city -> [ Key.INF; embassy'; country'; city.Name ] |> Key.wrap, city.Name))
            |> ResultAsync.map (Seq.sortBy fst)
            |> ResultAsync.map Map
            |> ResultAsync.map (fun data ->
                { Buttons.Name = $"My Cities"
                  Columns = 3
                  Data = data }
                |> Response.createButtons (chatId, msgId |> Replace))
