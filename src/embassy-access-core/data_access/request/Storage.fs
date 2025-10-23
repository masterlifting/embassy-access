[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Storage.Request

open Infrastructure.Domain
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open EA.Core.Domain
open EA.Core.DataAccess

type StorageType =
    | InMemory of InMemory.Connection
    | FileSystem of FileSystem.Connection
    | Postgre of Postgre.Connection

let private toProvider =
    function
    | Request.Storage.Provider(provider, payloadConverter) -> provider, payloadConverter

let init payloadConverter =
    fun storageType ->
        match storageType with
        | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
        | InMemory connection -> connection |> Storage.Connection.InMemory |> Storage.init
        | Postgre connection ->
            {
                Database.Database = Database.Postgre connection.String
                Database.Lifetime = connection.Lifetime
            }
            |> Storage.Connection.Database
            |> Storage.init
        |> Result.map (fun provider -> (provider, payloadConverter) |> Request.Storage.Provider)

module Query =

    type Filter = Id of RequestId

    type FilterSeq =
        | Ids of RequestId seq
        | ByEmbassyId of EmbassyId
        | ByServiceId of ServiceId
        | StartWithServiceId of ServiceId
        | ByEmbassyAndServiceId of EmbassyId * ServiceId

    let findOne filter storage =
        let provider, payloadMapper = storage |> toProvider

        let inMemoryQuery client =
            match filter with
            | Id id -> InMemory.Request.Query.tryFindById id
            |> fun find -> client |> find payloadMapper

        let fileSystemQuery client =
            match filter with
            | Id id -> FileSystem.Request.Query.tryFindById id
            |> fun find -> client |> find payloadMapper

        let postgreQuery client =
            match filter with
            | Id id -> Postgre.Request.Query.tryFindById id
            |> fun find -> client |> find payloadMapper

        match provider with
        | Storage.InMemory client -> client |> inMemoryQuery
        | Storage.FileSystem client -> client |> fileSystemQuery
        | Storage.Database(Database.Client.Postgre client) -> client |> postgreQuery
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let findMany filter storage =
        let provider, payloadMapper = storage |> toProvider

        let inMemoryQuery client =
            match filter with
            | Ids ids -> InMemory.Request.Query.findManyByIds ids
            | ByEmbassyId embassyId -> InMemory.Request.Query.findManyByEmbassyId embassyId
            | ByServiceId serviceId -> InMemory.Request.Query.findManyByServiceId serviceId
            | StartWithServiceId serviceId -> InMemory.Request.Query.findManyStartWithServiceId serviceId
            | ByEmbassyAndServiceId(embassyId, serviceId) ->
                InMemory.Request.Query.findManyByEmbassyIdAndServiceId embassyId serviceId
            |> fun find -> client |> find payloadMapper

        let fileSystemQuery client =
            match filter with
            | Ids ids -> FileSystem.Request.Query.findManyByIds ids
            | ByEmbassyId embassyId -> FileSystem.Request.Query.findManyByEmbassyId embassyId
            | ByServiceId serviceId -> FileSystem.Request.Query.findManyByServiceId serviceId
            | StartWithServiceId serviceId -> FileSystem.Request.Query.findManyStartWithServiceId serviceId
            | ByEmbassyAndServiceId(embassyId, serviceId) ->
                FileSystem.Request.Query.findManyByEmbassyIdAndServiceId embassyId serviceId
            |> fun find -> client |> find payloadMapper

        let postgreQuery client =
            match filter with
            | Ids ids -> Postgre.Request.Query.findManyByIds ids
            | ByEmbassyId embassyId -> Postgre.Request.Query.findManyByEmbassyId embassyId
            | ByServiceId serviceId -> Postgre.Request.Query.findManyByServiceId serviceId
            | StartWithServiceId serviceId -> Postgre.Request.Query.findManyStartWithServiceId serviceId
            | ByEmbassyAndServiceId(embassyId, serviceId) ->
                Postgre.Request.Query.findManyByEmbassyIdAndServiceId embassyId serviceId
            |> fun find -> client |> find payloadMapper

        match provider with
        | Storage.InMemory client -> client |> inMemoryQuery
        | Storage.FileSystem client -> client |> fileSystemQuery
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> postgreQuery
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

module Command =

    let upsert request storage =
        let provider, payloadMapper = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.upsert request payloadMapper
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.upsert request payloadMapper
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> Postgre.Request.Command.upsert request payloadMapper
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let delete id storage =
        let provider, _ = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.delete id
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.delete id
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> Postgre.Request.Command.delete id
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
