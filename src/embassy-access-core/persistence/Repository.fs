[<RequireQualifiedAccess>]
module EA.Persistence.Repository

open Infrastructure
open Persistence.Domain
open EA.Persistence
open Infrastructure.Logging

module Query =
    module Request =
        let getOne query ct client =
            Log.trace $"InMemory query request {query}"

            match client with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Request.getOne query ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Request.getOne query ct
            | _ -> $"Storage {client}" |> NotSupported |> Error |> async.Return

        let getMany query ct storage =
            Log.trace $"InMemory query request {query}"

            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Request.getMany query ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Request.getMany query ct
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Command =
    module Request =
        let execute storage ct command =
            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Command.Request.execute command ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Command.Request.execute command ct
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

        let update request ct storage =
            request |> Command.Request.Update |> execute storage ct
