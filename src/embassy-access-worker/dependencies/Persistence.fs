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

module Russian =
    open EA.Russian.Services.DataAccess
    open EA.Russian.Services.DataAccess.Kdmid
    open EA.Russian.Services.DataAccess.Midpass

    type Dependencies = {
        initKdmidRequestStorage: unit -> Result<Kdmid.StorageType, Error'>
        initMidpassRequestStorage: unit -> Result<Midpass.StorageType, Error'>
    } with

        static member create fileStoragePath =
            let initKdmidRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "requests-rus-kdmid.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init {
                    toDomain = Kdmid.Payload.toDomain
                    toEntity = Kdmid.Payload.toEntity
                }

            let initMidpassRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "requests-rus-midpass.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init {
                    toDomain = Midpass.Payload.toDomain
                    toEntity = Midpass.Payload.toEntity
                }

            {
                initKdmidRequestStorage = initKdmidRequestStorage
                initMidpassRequestStorage = initMidpassRequestStorage
            }

module Italian =
    open EA.Italian.Services.DataAccess
    open EA.Italian.Services.DataAccess.Prenotami

    type Dependencies = {
        initPrenotamiRequestStorage: unit -> Result<Prenotami.StorageType, Error'>
    } with

        static member create fileStoragePath fileStorageKey =
            let initPrenotamiRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "requests-ita-prenotami.json"
                }
                |> Storage.Request.FileSystem
                |> Storage.Request.init {
                    toDomain = Prenotami.Payload.toDomain fileStorageKey
                    toEntity = Prenotami.Payload.toEntity fileStorageKey
                }

            {
                initPrenotamiRequestStorage = initPrenotamiRequestStorage
            }

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initServiceStorage: unit -> Result<ServicesTree.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassiesTree.Storage, Error'>
    initCultureStorage: unit -> Result<Culture.Storage, Error'>
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
            |> Storage.Culture.FileSystem
            |> Storage.Culture.init

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
            |> EmbassiesTree.Configuration
            |> EmbassiesTree.init

        let initServiceStorage () =
            {
                Configuration.Connection.Provider = cfg
                Configuration.Connection.Section = "Services"
            }
            |> ServicesTree.Configuration
            |> ServicesTree.init

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
