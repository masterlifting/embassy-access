[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open System
open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Storages
open Persistence.Storages.Domain
open AIProvider.Services.DataAccess
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.DataAccess
open EA.Russian.Services.Domain
open EA.Italian.Services.Domain

let private result = ResultBuilder()
let private resultAsync = ResultAsyncBuilder()

let private cleanData subscriptions =
    fun (chatStorage, requestStorage) ->
        resultAsync {

            let! requestIdentifiers =
                requestStorage
                |> Storage.Request.Query.getIdentifiers
                |> ResultAsync.map Set.ofSeq

            let requestIdsToRemove = subscriptions |> Set.difference <| requestIdentifiers

            do! chatStorage |> Storage.Chat.Command.deleteSubscriptions requestIdsToRemove
            return requestStorage |> Storage.Request.Command.deleteMany requestIdsToRemove
        }

module Russian =

    type Dependencies = {
        initKdmidRequestStorage: unit -> Result<Request.Storage<Kdmid.Payload>, Error'>
        cleanData: Set<RequestId> -> Chat.Storage -> Async<Result<unit, Error'>>
    } with

        static member create fileStoragePath =
            let initKdmidRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Requests.Rus.Kdmid.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init (Kdmid.Payload.serialize, Kdmid.Payload.deserialize)

            result {

                let! kdmidRequestStorage = initKdmidRequestStorage ()

                let cleanData subscriptions chatStorage =
                    (chatStorage, kdmidRequestStorage) |> cleanData subscriptions

                return {
                    initKdmidRequestStorage = fun () -> kdmidRequestStorage |> Ok
                    cleanData = cleanData
                }
            }

module Italian =

    type Dependencies = {
        initPrenotamiRequestStorage: unit -> Result<Request.Storage<Prenotami.Payload>, Error'>
        cleanData: Set<RequestId> -> Chat.Storage -> Async<Result<unit, Error'>>
    } with

        static member create fileStoragePath fileStorageKey =
            let initPrenotamiRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Requests.Ita.Prenotami.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init (
                    Prenotami.Payload.serialize fileStorageKey,
                    Prenotami.Payload.deserialize fileStorageKey
                )

            result {
                let! prenotamiRequestStorage = initPrenotamiRequestStorage ()

                let cleanData subscriptions chatStorage =
                    (chatStorage, prenotamiRequestStorage) |> cleanData subscriptions

                return {
                    initPrenotamiRequestStorage = fun () -> prenotamiRequestStorage |> Ok
                    cleanData = cleanData
                }
            }

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initServiceStorage: unit -> Result<ServiceGraph.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassyGraph.Storage, Error'>
    initCultureStorage: unit -> Result<Culture.Response.Storage, Error'>
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
    cleanData: unit -> Async<Result<unit, Error'>>
} with

    static member create cfg =

        let inline getEnv name =
            Some cfg
            |> Configuration.Client.tryGetEnv name
            |> Result.bind (function
                | Some value -> Ok value
                | None -> $"Environment configuration '{name}' not found." |> NotFound |> Error)

        result {

            let! fileStoragePath = getEnv "Persistence:FileSystem"
            let! fileStorageKey = getEnv "Persistence:Key"

            let initCultureStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Culture.json"
                }
                |> Culture.Response.FileSystem
                |> Culture.Response.init

            let initChatStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "Chats.json"
                }
                |> Storage.Chat.FileSystem
                |> Storage.Chat.init

            let initEmbassyStorage () =
                {
                    Configuration.Connection.Provider = cfg
                    Configuration.Connection.Section = "Embassies"
                }
                |> EmbassyGraph.Configuration
                |> EmbassyGraph.init

            let initServiceStorage () =
                {
                    Configuration.Connection.Provider = cfg
                    Configuration.Connection.Section = "Services"
                }
                |> ServiceGraph.Configuration
                |> ServiceGraph.init

            let! chatStorage = initChatStorage ()
            let! russianStorage = Russian.Dependencies.create fileStoragePath
            let! italianStorage = Italian.Dependencies.create fileStoragePath fileStorageKey

            let cleanData () =
                resultAsync {
                    let! subscriptions = chatStorage |> Storage.Chat.Query.getSubscriptions |> ResultAsync.map Set.ofSeq
                    do! chatStorage |> russianStorage.cleanData subscriptions
                    return chatStorage |> italianStorage.cleanData subscriptions
                }

            return {
                initServiceStorage = initServiceStorage
                initEmbassyStorage = initEmbassyStorage
                initCultureStorage = initCultureStorage
                initChatStorage = fun () -> chatStorage |> Ok
                RussianStorage = russianStorage
                ItalianStorage = italianStorage
                cleanData = cleanData
            }
        }
