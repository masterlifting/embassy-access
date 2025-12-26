module internal EA.Worker.Features.Embassies.Italian.Prenotami.Infra

open Persistence.Domain
open EA.Italian.Domain.Prenotami

module RequestStorage =
    open EA.Core.DataAccess
    open EA.Italian.DataAccess.Prenotami

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
