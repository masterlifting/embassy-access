[<RequireQualifiedAccess>]
module EA.Core.DataAccess.Storage.Request

open Infrastructure.Domain
open EA.Core.Domain
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open EA.Core.DataAccess

type StorageType =
    | InMemory of InMemory.Connection
    | FileSystem of FileSystem.Connection

let private toProvider =
    function
    | Request.Storage.Provider(provider, payloadConverter) -> provider, payloadConverter

let init payloadConverter =
    fun storageType ->
        match storageType with
        | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
        | InMemory connection -> connection |> Storage.Connection.InMemory |> Storage.init
        |> Result.map (fun provider -> (provider, payloadConverter) |> Request.Storage.Provider)

module Query =

    type Filter = Id of RequestId

    type FilterSeq =
        | Ids of RequestId seq
        | ByEmbassyId of EmbassyId
        | ByServiceId of ServiceId
        | StartWithServiceId of ServiceId
        | ByEmbassyAndServiceId of EmbassyId * ServiceId

    let getIdentifiers storage =
        let provider, _ = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Query.getIdentifiers
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.getIdentifiers
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let tryFind filter storage =
        let provider, payloadMapper = storage |> toProvider

        let inMemoryQuery client =
            match filter with
            | Id id -> InMemory.Request.Query.tryFindById id
            |> fun find -> client |> find payloadMapper

        let fileSystemQuery client =
            match filter with
            | Id id -> FileSystem.Request.Query.tryFindById id
            |> fun find -> client |> find payloadMapper

        match provider with
        | Storage.InMemory client -> client |> inMemoryQuery
        | Storage.FileSystem client -> client |> fileSystemQuery
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

        match provider with
        | Storage.InMemory client -> client |> inMemoryQuery
        | Storage.FileSystem client -> client |> fileSystemQuery
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

module Command =

    let create request storage =
        let provider, payloadMapper = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.create request payloadMapper
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.create request payloadMapper
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let update request storage =
        let provider, payloadMapper = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.update request payloadMapper
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.update request payloadMapper
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let updateSeq requests storage =
        let provider, payloadMapper = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.updateSeq requests payloadMapper
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.updateSeq requests payloadMapper
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let createOrUpdate request storage =
        let provider, payloadMapper = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.createOrUpdate request payloadMapper
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.createOrUpdate request payloadMapper
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let delete id storage =
        let provider, _ = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.delete id
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.delete id
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let deleteMany ids storage =
        let provider, _ = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.deleteMany ids
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.deleteMany ids
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
