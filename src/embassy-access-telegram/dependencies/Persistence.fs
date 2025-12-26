[<RequireQualifiedAccess>]
module EA.Telegram.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Domain
open AIProvider.Features.DataAccess
open EA.Core.DataAccess
open EA.Telegram.Domain
open EA.Telegram.DataAccess

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
                Storage.Request.Postgre {
                    String = pgConnectionString
                    Lifetime = Transient
                }
                |> Storage.Request.init {
                    toDomain = Kdmid.Payload.toDomain
                    toEntity = Kdmid.Payload.toEntity
                }

            let initMidpassRequestStorage () =
                Storage.Request.Postgre {
                    String = pgConnectionString
                    Lifetime = Transient
                }
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
                Storage.Request.Postgre {
                    String = pgConnectionString
                    Lifetime = Transient
                }
                |> Storage.Request.init {
                    toDomain = Prenotami.Payload.toDomain fileStorageKey
                    toEntity = Prenotami.Payload.toEntity fileStorageKey
                }

            {
                initPrenotamiRequestStorage = initPrenotamiRequestStorage
            }

type Dependencies = {
    initChatStorage: unit -> Result<Chat.Storage, Error'>
    initCultureStorage: unit -> Result<Culture.Storage, Error'>
    initServiceStorage: unit -> Result<ServicesTree.Storage, Error'>
    initEmbassyStorage: unit -> Result<EmbassiesTree.Storage, Error'>
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
} with

    static member create cfg =

        let initChatStorage () =
            Storage.Chat.Postgre {
                String = EA.Telegram.Shared.Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Transient
            }
            |> Storage.Chat.init

        let initCultureStorage () =
            Storage.Culture.Postgre {
                String = EA.Telegram.Shared.Configuration.ENVIRONMENTS.PostgresConnection
                Lifetime = Transient
            }
            |> Storage.Culture.init

        let initEmbassyStorage () =
            EmbassiesTree.Configuration {
                Provider = cfg
                Section = "Embassies"
            }
            |> EmbassiesTree.init

        let initServiceStorage () =
            ServicesTree.Configuration { Provider = cfg; Section = "Services" }
            |> ServicesTree.init

        result {

            let pgConnectionString =
                EA.Telegram.Shared.Configuration.ENVIRONMENTS.PostgresConnection
            let fileStorageKey = EA.Telegram.Shared.Configuration.ENVIRONMENTS.EncryptionKey

            return {
                initChatStorage = initChatStorage
                initCultureStorage = initCultureStorage
                initServiceStorage = initServiceStorage
                initEmbassyStorage = initEmbassyStorage
                RussianStorage = Russian.Dependencies.create pgConnectionString
                ItalianStorage = Italian.Dependencies.create pgConnectionString fileStorageKey
            }
        }
