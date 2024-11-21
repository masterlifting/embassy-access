module EA.Telegram.CommandHandler.Common

open System
open Infrastructure
open EA.Telegram
open EA.Core.Domain
open Web.Telegram.Producer
open Web.Telegram.Domain.Producer

open EA.Embassies.Russian

let private getAll () =
    API.SUPPORTED_CITIES |> Seq.map Russian |> Set.ofSeq

let embassies chatId =
    getAll ()
    |> Seq.map EA.Core.Mapper.Embassy.toExternal
    |> Seq.groupBy _.Name
    |> Seq.map fst
    |> Seq.sort
    |> Seq.map (fun embassyName -> embassyName |> Command.Countries |> Command.set, embassyName)
    |> Map
    |> fun data ->
        { Buttons.Name = "Which embassy do you need?"
          Columns = 3
          Data = data }
        |> Buttons.create (chatId, New)
    |> Ok
    |> async.Return

let countries embassyName =
    fun (chatId, msgId) ->
        let data =
            getAll ()
            |> Seq.map EA.Core.Mapper.Embassy.toExternal
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
        getAll ()
        |> Seq.map EA.Core.Mapper.Embassy.toExternal
        |> Seq.filter (fun embassy -> embassy.Name = embassyName && embassy.Country.Name = countryName)
        |> Seq.groupBy _.Country.City.Name
        |> Seq.sortBy fst
        |> Seq.collect (fun (_, embassies) -> embassies |> Seq.take 1)
        |> Seq.map (fun embassy ->
            embassy
            |> EA.Core.Mapper.Embassy.toInternal
            |> Result.map (fun x -> x |> Command.Service |> Command.set, embassy.Country.City.Name))
        |> Result.choose
        |> Result.map Map
        |> Result.map (fun data ->
            { Buttons.Name = $"Which city in {countryName}?"
              Columns = 3
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))
        |> async.Return

open EA.Telegram.Persistence

let userEmbassies chatId =
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
            |> Seq.map EA.Core.Mapper.Embassy.toExternal
            |> Seq.groupBy _.Name
            |> Seq.map fst
            |> Seq.sort
            |> Seq.map (fun embassyName -> embassyName |> Command.UserCountries |> Command.set, embassyName)
            |> Map)
        |> ResultAsync.map (fun data ->
            { Buttons.Name = "Choose embassy you are following"
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
        |> ResultAsync.map (Seq.map EA.Core.Mapper.Embassy.toExternal)
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
            |> Seq.map EA.Core.Mapper.Embassy.toExternal
            |> Seq.filter (fun embassy -> embassy.Name = embassyName && embassy.Country.Name = countryName)
            |> Seq.groupBy _.Country.City.Name
            |> Seq.sortBy fst
            |> Seq.collect (fun (_, embassies) -> embassies |> Seq.take 1)
            |> Seq.map (fun embassy ->
                embassy
                |> EA.Core.Mapper.Embassy.toInternal
                |> Result.map (fun x -> x |> Command.UserSubscriptions |> Command.set, embassy.Country.City.Name))
            |> Result.choose
            |> Result.map Map)
        |> ResultAsync.map (fun data ->
            { Buttons.Name = $"Which city in {countryName}?"
              Columns = 3
              Data = data }
            |> Buttons.create (chatId, msgId |> Replace))

let service embassy =
    fun (chatId, msgId) ->
        match embassy with
        | Russian _ ->
            EA.Telegram.CommandHandler.Russian.services ()
            |> Seq.map (fun service -> (embassy, service, 0uy) |> Command.RussianService |> Command.set, service)
            |> Map
            |> fun data ->
                { Buttons.Name = "What do you need?"
                  Columns = 3
                  Data = data }
                |> Buttons.create (chatId, msgId |> Replace)
                |> Ok
                |> async.Return
        | _ -> $"Service for {embassy}" |> NotSupported |> Error |> async.Return
        