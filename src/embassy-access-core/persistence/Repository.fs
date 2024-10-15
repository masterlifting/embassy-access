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
        let execute ct command storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Request.execute ct command
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Command.Request.execute ct command
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let update ct storage request =
            let operation =
                request |> Command.Definitions.Request.Update.Request |> Command.Request.Update

            storage |> execute ct operation
