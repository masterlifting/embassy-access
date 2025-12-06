module internal EA.Worker.Dependencies.Embassies.RussianInfra

open Persistence.Domain
open EA.Core.DataAccess
open EA.Russian.Services.Domain.Kdmid
open EA.Russian.Services.DataAccess.Kdmid

let initRequestStorage connectionString =
    Storage.Request.Postgre {
        String = connectionString
        Lifetime = Transient
    }
    |> Storage.Request.init {
        toDomain = Payload.toDomain
        toEntity = Payload.toEntity
    }

let disposeRequestStorage storage =
    storage |> Storage.Request.dispose |> Ok
