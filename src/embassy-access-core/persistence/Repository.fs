[<RequireQualifiedAccess>]
module EA.Persistence.Repository

open Infrastructure
open Persistence.Domain
open EA.Persistence
open Infrastructure.Logging

module Query =
    module Request =
        let getOne query ct storageType =
            Log.trace $"InMemory query request {query}"

            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Request.getOne query ct
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Request.getOne query ct
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany query ct storageType =
            Log.trace $"InMemory query request {query}"

            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Request.getMany query ct
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Request.getMany query ct
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

module Command =
    module Request =
        let execute command ct storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Request.execute command ct
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Command.Request.execute command ct
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let update request ct storage =
            let command =
                request |> Command.Definitions.Request.Update.Request |> Command.Request.Update

            storage |> execute command ct
