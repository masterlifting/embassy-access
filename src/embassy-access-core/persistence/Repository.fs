[<RequireQualifiedAccess>]
module EA.Persistence.Repository

open Infrastructure
open Persistence.Domain
open EA.Persistence
open Infrastructure.Logging

module Query =
    module Request =
        let getOne ct query storageType =
            Log.trace $"InMemory query request {query}"

            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Request.getOne ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Request.getOne ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany ct query storageType =
            Log.trace $"InMemory query request {query}"

            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Request.getMany ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Request.getMany ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

module Command =
    module Request =
        let execute ct operation storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Request.execute ct operation
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Command.Request.execute ct operation
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return
