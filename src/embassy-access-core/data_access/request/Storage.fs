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
    
let private toStorage =
    function
    | Request.Storage storage -> storage

let init storageType =
    fun serializePayload deserializePayload ->
        match storageType with
        | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
        | InMemory connection -> connection |> Storage.Connection.InMemory |> Storage.init
        |> Result.map (fun provider ->
            Request.Storage {|
                Provider = provider
                serializePayload = serializePayload
                deserializePayload = deserializePayload
            |})

module Query =

    let getIdentifiers table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Query.getIdentifiers
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.getIdentifiers
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let tryFindById id table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Query.tryFindById id storage.deserializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.tryFindById id storage.deserializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByEmbassyId embassyId table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Query.findManyByEmbassyId embassyId storage.deserializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.findManyByEmbassyId embassyId storage.deserializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByServiceId id table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Query.findManyByServiceId id storage.deserializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.findManyByServiceId id storage.deserializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyWithServiceId id table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Query.findManyWithServiceId id storage.deserializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.findManyWithServiceId id storage.deserializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByIds ids table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Query.findManyByIds ids storage.deserializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.findManyByIds ids storage.deserializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

module Command =

    let create request table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.create request storage.serializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.create request storage.serializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let update request table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.update request storage.serializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.update request storage.serializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let updateSeq requests table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.updateSeq requests storage.serializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.updateSeq requests storage.serializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let createOrUpdate request table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.createOrUpdate request storage.serializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.createOrUpdate request storage.serializePayload
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let delete id table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.delete id
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.delete id
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return

    let deleteMany ids table =
        let storage = table |> toStorage
        match storage.Provider with
        | Storage.InMemory client -> client |> InMemory.Request.Command.deleteMany ids
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.deleteMany ids
        | _ -> $"The '{storage}' is not supported." |> NotSupported |> Error |> async.Return
