module internal EA.Worker.Embassies.Italian.Prenotami.Infra

open Persistence.Domain
open EA.Italian.Services.Domain.Prenotami

module RequestStorage =
    open EA.Core.DataAccess
    open EA.Italian.Services.DataAccess.Prenotami

    let init connectionString encryptionKey =
        Storage.Request.Postgre {
            String = connectionString
            Lifetime = Transient
        }
        |> Storage.Request.init {
            toDomain = Payload.toDomain encryptionKey
            toEntity = Payload.toEntity encryptionKey
        }

    let dispose storage =
        storage |> Storage.Request.dispose |> Ok
