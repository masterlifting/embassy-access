[<RequireQualifiedAccess>]
module EA.Core.Persistence.Repository

open Infrastructure
open Persistence.Domain
open EA.Persistence
open Infrastructure.Logging

module Query =
    module Request =
        let tryFindOne storage ct query =
            Log.trace $"InMemory query request {query}"

            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Request.tryFindOne query ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Request.trytFindOne query ct
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

        let findMany storage ct query =
            Log.trace $"InMemory query request {query}"

            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Request.findMany query ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Request.findMany query ct
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Command =
    module Request =
        let execute storage ct operation =
            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Command.Request.execute operation ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Command.Request.execute operation ct
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
