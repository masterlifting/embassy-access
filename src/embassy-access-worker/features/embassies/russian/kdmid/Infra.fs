module internal EA.Worker.Features.Embassies.Russian.Kdmid.Infra

open Persistence.Domain
open EA.Russian.Services.Domain.Kdmid

module RequestStorage =
    open EA.Core.DataAccess
    open EA.Russian.Services.DataAccess.Kdmid

    let init connectionString =
        Storage.Request.Postgre {
            String = connectionString
            Lifetime = Transient
        }
        |> Storage.Request.init {
            toDomain = Payload.toDomain
            toEntity = Payload.toEntity
        }

    let dispose storage =
        storage |> Storage.Request.dispose |> Ok
