[<RequireQualifiedAccess>]
module EA.Worker.Dependencies.Persistence

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

        static member create connectionString =
            let initKdmidRequestStorage () =
                Storage.Request.Postgre {
                    String = connectionString
                    Lifetime = Transient
                }
                |> Storage.Request.init {
                    toDomain = Kdmid.Payload.toDomain
                    toEntity = Kdmid.Payload.toEntity
                }

            let initMidpassRequestStorage () =
                Storage.Request.Postgre {
                    String = connectionString
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

        static member create connectionString encryptionKey =
            let initPrenotamiRequestStorage () =
                Storage.Request.Postgre {
                    String = connectionString
                    Lifetime = Transient
                }
                |> Storage.Request.init {
                    toDomain = Prenotami.Payload.toDomain encryptionKey
                    toEntity = Prenotami.Payload.toEntity encryptionKey
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

            let connectionString = Configuration.ENVIRONMENTS.PostgresConnection
            let encryptionKey = Configuration.ENVIRONMENTS.EncryptionKey

            return {
                RussianStorage = Russian.Dependencies.create connectionString
                ItalianStorage = Italian.Dependencies.create connectionString encryptionKey
            }
        }
