[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Repository

open Infrastructure
open Infrastructure.Logging
open Persistence.Domain

module Query =
    module Chat =
        Log.trace $"InMemory query request {query}"

        let getOne ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Chat.getOne ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Chat.getOne ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany ct query storageType =
            Log.trace $"InMemory query request {query}"

            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Chat.getMany ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Chat.getMany ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

module Command =
    module Chat =
        let execute ct operation storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Chat.execute ct operation
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Command.Chat.execute ct operation
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return
