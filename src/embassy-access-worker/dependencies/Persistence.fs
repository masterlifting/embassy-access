[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open System
open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Domain
open Persistence.Storages.Domain
open EA.Core.DataAccess
open EA.Russian.Services.Domain
open EA.Italian.Services.Domain
open EA.Worker.Dependencies

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
                    FileSystem.Connection.Lifetime = Transient
                }
                |> Storage.Request.StorageType.FileSystem
                |> Storage.Request.init {
                    toDomain = Kdmid.Payload.toDomain
                    toEntity = Kdmid.Payload.toEntity
                }

            let initMidpassRequestStorage () =
                {
                    FileSystem.Connection.FilePath = fileStoragePath
                    FileSystem.Connection.FileName = "requests-rus-midpass.json"
                    FileSystem.Connection.Lifetime = Transient
                }
                |> Storage.Request.StorageType.FileSystem
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
                    FileSystem.Connection.Lifetime = Transient
                }
                |> Storage.Request.StorageType.FileSystem
                |> Storage.Request.init {
                    toDomain = Prenotami.Payload.toDomain fileStorageKey
                    toEntity = Prenotami.Payload.toEntity fileStorageKey
                }

            {
                initPrenotamiRequestStorage = initPrenotamiRequestStorage
            }

type Dependencies = {
    initServiceStorage: unit -> Result<ServicesTree.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassiesTree.Storage, Error'>
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
} with

    static member create cfg =

        let initEmbassyStorage () =
            {
                Configuration.Connection.Provider = cfg
                Configuration.Connection.Section = "Embassies"
            }
            |> EmbassiesTree.StorageType.Configuration
            |> EmbassiesTree.init

        let initServiceStorage () =
            {
                Configuration.Connection.Provider = cfg
                Configuration.Connection.Section = "Services"
            }
            |> ServicesTree.StorageType.Configuration
            |> ServicesTree.init

        result {

            let fileStoragePath = "data"
            let fileStorageKey = Configuration.ENVIRONMENTS.DataEncryptionKey

            return {
                initServiceStorage = initServiceStorage
                initEmbassyStorage = initEmbassyStorage
                RussianStorage = Russian.Dependencies.create fileStoragePath
                ItalianStorage = Italian.Dependencies.create fileStoragePath fileStorageKey
            }
        }
