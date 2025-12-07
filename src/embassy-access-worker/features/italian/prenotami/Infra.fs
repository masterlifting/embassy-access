module internal EA.Worker.Features.Italian.Prenotami.Infra

open Persistence.Domain
open EA.Core.DataAccess
open EA.Italian.Services.Domain.Prenotami
open EA.Italian.Services.DataAccess.Prenotami

let initRequestStorage connectionString encryptionKey =
    Storage.Request.Postgre {
        String = connectionString
        Lifetime = Transient
    }
    |> Storage.Request.init {
        toDomain = Payload.toDomain encryptionKey
        toEntity = Payload.toEntity encryptionKey
    }

let disposeRequestStorage storage =
    storage |> Storage.Request.dispose |> Ok
