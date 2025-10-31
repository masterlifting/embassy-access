[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open Infrastructure
open Infrastructure.Domain
open Infrastructure.Prelude
open Infrastructure.Configuration.Domain
open Persistence.Domain
open Persistence.Storages.Domain
open EA.Core.Domain
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Dependencies

let private result = ResultBuilder()

module Russian =
    open EA.Russian.Services.Domain
    open EA.Russian.Services.DataAccess
    open EA.Russian.Services.DataAccess.Kdmid
    open EA.Russian.Services.DataAccess.Midpass

    type Dependencies = {
        initKdmidRequestStorage: unit -> Result<Kdmid.StorageType, Error'>
        initMidpassRequestStorage: unit -> Result<Midpass.StorageType, Error'>
    } with

        static member create pgConnectionString =
            let initKdmidRequestStorage () =
                ({
                    String = pgConnectionString
                    Lifetime = Transient
                }
                : Postgre.Connection)
                |> Storage.Request.StorageType.Postgre
                |> Storage.Request.init {
                    toDomain = Kdmid.Payload.toDomain
                    toEntity = Kdmid.Payload.toEntity
                }

            let initMidpassRequestStorage () =
                ({
                    String = pgConnectionString
                    Lifetime = Transient
                }
                : Postgre.Connection)
                |> Storage.Request.StorageType.Postgre
                |> Storage.Request.init {
                    toDomain = Midpass.Payload.toDomain
                    toEntity = Midpass.Payload.toEntity
                }

            {
                initKdmidRequestStorage = initKdmidRequestStorage
                initMidpassRequestStorage = initMidpassRequestStorage
            }

module Italian =
    open EA.Italian.Services.Domain
    open EA.Italian.Services.DataAccess
    open EA.Italian.Services.DataAccess.Prenotami

    type Dependencies = {
        initPrenotamiRequestStorage: unit -> Result<Prenotami.StorageType, Error'>
    } with

        static member create pgConnectionString fileStorageKey =
            let initPrenotamiRequestStorage () =
                ({
                    String = pgConnectionString
                    Lifetime = Transient
                }
                : Postgre.Connection)
                |> Storage.Request.StorageType.Postgre
                |> Storage.Request.init {
                    toDomain = Prenotami.Payload.toDomain fileStorageKey
                    toEntity = Prenotami.Payload.toEntity fileStorageKey
                }

            {
                initPrenotamiRequestStorage = initPrenotamiRequestStorage
            }

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initCultureStorage: unit -> Result<AIProvider.Services.DataAccess.Culture.Storage, Error'>
    initServiceStorage: unit -> Result<ServicesTree.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassiesTree.Storage, Error'>
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
} with

    static member create cfg =

        let initChatStorage () =
            ({
                String = Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Singleton
            }
            : Postgre.Connection)
            |> Storage.Chat.StorageType.Postgre
            |> Storage.Chat.init

        let initCultureStorage () =
            ({
                FilePath = "./data"
                FileName = "culture"
                Lifetime = Singleton
            }
            : FileSystem.Connection)
            |> AIProvider.Services.DataAccess.Storage.Culture.StorageType.FileSystem
            |> AIProvider.Services.DataAccess.Storage.Culture.init

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

            let pgConnectionString = Configuration.ENVIRONMENTS.PostgresConnection
            let fileStorageKey = Configuration.ENVIRONMENTS.DataEncryptionKey

            return {
                initChatStorage = initChatStorage
                initCultureStorage = initCultureStorage
                initServiceStorage = initServiceStorage
                initEmbassyStorage = initEmbassyStorage
                RussianStorage = Russian.Dependencies.create pgConnectionString
                ItalianStorage = Italian.Dependencies.create pgConnectionString fileStorageKey
            }
        }
