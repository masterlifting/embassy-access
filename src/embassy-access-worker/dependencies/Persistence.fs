[<RequireQualifiedAccess>]
module internal EA.Worker.Dependencies.Persistence

open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence.Domain
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
    RussianStorage: Russian.Dependencies
    ItalianStorage: Italian.Dependencies
} with

    static member create() =
        result {

            let pgConnectionString = Configuration.ENVIRONMENTS.PostgresConnection
            let fileStorageKey = Configuration.ENVIRONMENTS.EncryptionKey

            return {
                RussianStorage = Russian.Dependencies.create pgConnectionString
                ItalianStorage = Italian.Dependencies.create pgConnectionString fileStorageKey
            }
        }
