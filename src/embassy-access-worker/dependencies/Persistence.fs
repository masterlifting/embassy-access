[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open System
open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Storages.Domain
open AIProvider.Services.DataAccess
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Russian.Services.Domain
open EA.Italian.Services.Domain

let private result = ResultBuilder()
let private resultAsync = ResultAsyncBuilder()

module Russian =
    open EA.Russian.Services.DataAccess.Kdmid

    type Dependencies = {
        initKdmidRequestStorage: unit -> Result<Request.Storage<Kdmid.Payload>, Error'>
        initMidpassRequestStorage: unit -> Result<Request.Storage<Midpass.Payload>, Error'>
    } with

        static member create fileStoragePath =
            let initKdmidRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "requests-rus-kdmid.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init (Kdmid.Payload.serialize, Kdmid.Payload.deserialize)

            let initMidpassRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "requests-rus-midpass.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init (Midpass.Payload.serialize, Midpass.Payload.deserialize)

            {
                initKdmidRequestStorage = initKdmidRequestStorage
                initMidpassRequestStorage = initMidpassRequestStorage
            }

module Italian =
    open EA.Italian.Services.DataAccess.Prenotami

    type Dependencies = {
        initPrenotamiRequestStorage: unit -> Result<Request.Storage<Prenotami.Payload>, Error'>
    } with

        static member create fileStoragePath fileStorageKey =
            let initPrenotamiRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "requests-ita-prenotami.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init (
                    Prenotami.Payload.serialize fileStorageKey,
                    Prenotami.Payload.deserialize fileStorageKey
                )

            {
                initPrenotamiRequestStorage = initPrenotamiRequestStorage
            }

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initServiceStorage: unit -> Result<ServiceGraph.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassyGraph.Storage, Error'>
    initCultureStorage: unit -> Result<Culture.Response.Storage, Error'>
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
} with

    static member create cfg =

        let getEnv name =
            Some cfg
            |> Configuration.Client.tryGetEnv name
            |> Result.bind (function
                | Some value -> Ok value
                | None -> $"Environment configuration '{name}' not found." |> NotFound |> Error)

        let initCultureStorage fileStoragePath =
            {
                FileSystem.Connection.FilePath = fileStoragePath
                FileSystem.Connection.FileName = "culture.json"
            }
            |> Culture.Response.FileSystem
            |> Culture.Response.init

        let initChatStorage fileStoragePath =
            {
                FileSystem.Connection.FilePath = fileStoragePath
                FileSystem.Connection.FileName = "chats.json"
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

        result {

            let! fileStoragePath = getEnv "Persistence:FileSystem"
            let! fileStorageKey = getEnv "Persistence:Key"

            let initCultureStorage () = initCultureStorage fileStoragePath
            let initChatStorage () = initChatStorage fileStoragePath

            return {
                initServiceStorage = initServiceStorage
                initEmbassyStorage = initEmbassyStorage
                initCultureStorage = initCultureStorage
                initChatStorage = initChatStorage
                RussianStorage = Russian.Dependencies.create fileStoragePath
                ItalianStorage = Italian.Dependencies.create fileStoragePath fileStorageKey
            }
        }
