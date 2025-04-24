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
    | Request.Provider provider -> provider

let init (serializePayload, deserializePayload) =
    fun storageType ->
        match storageType with
        | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
        | InMemory connection -> connection |> Storage.Connection.InMemory |> Storage.init
        |> Result.map (fun provider ->
            Request.Provider {|
                Type = provider
                serializePayload = serializePayload
                deserializePayload = deserializePayload
            |})

module Query =

    let getIdentifiers storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Query.getIdentifiers
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.getIdentifiers
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let tryFindById id storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Query.tryFindById id provider.deserializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.tryFindById id provider.deserializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByEmbassyId embassyId storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client ->
            client
            |> InMemory.Request.Query.findManyByEmbassyId embassyId provider.deserializePayload
        | Storage.FileSystem client ->
            client
            |> FileSystem.Request.Query.findManyByEmbassyId embassyId provider.deserializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByServiceId id storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client ->
            client
            |> InMemory.Request.Query.findManyByServiceId id provider.deserializePayload
        | Storage.FileSystem client ->
            client
            |> FileSystem.Request.Query.findManyByServiceId id provider.deserializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyWithServiceId id storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client ->
            client
            |> InMemory.Request.Query.findManyWithServiceId id provider.deserializePayload
        | Storage.FileSystem client ->
            client
            |> FileSystem.Request.Query.findManyWithServiceId id provider.deserializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyByIds ids storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Query.findManyByIds ids provider.deserializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Query.findManyByIds ids provider.deserializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

module Command =

    let create request storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Command.create request provider.serializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.create request provider.serializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let update request storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Command.update request provider.serializePayload
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.update request provider.serializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let updateSeq requests storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Command.updateSeq requests provider.serializePayload
        | Storage.FileSystem client ->
            client
            |> FileSystem.Request.Command.updateSeq requests provider.serializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let createOrUpdate request storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client ->
            client
            |> InMemory.Request.Command.createOrUpdate request provider.serializePayload
        | Storage.FileSystem client ->
            client
            |> FileSystem.Request.Command.createOrUpdate request provider.serializePayload
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let delete id storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Command.delete id
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.delete id
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let deleteMany ids storage =
        let provider = storage |> toProvider
        match provider.Type with
        | Storage.InMemory client -> client |> InMemory.Request.Command.deleteMany ids
        | Storage.FileSystem client -> client |> FileSystem.Request.Command.deleteMany ids
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
