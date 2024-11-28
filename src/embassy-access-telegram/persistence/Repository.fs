[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Repository

open Infrastructure
open Infrastructure.Logging
open Persistence.Domain

module Query =
    module Chat =

        let tryFindOne storage ct query =
            Log.trace $"InMemory query request {query}"

            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Chat.tryFindOne ct query
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Chat.tryFindOne ct query
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

        let findMany storage ct query =
            Log.trace $"InMemory query request {query}"

            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Chat.findMany ct query
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Chat.findMany ct query
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Command =
    module Chat =
        let execute storage ct operation =
            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Command.Chat.execute operation ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Command.Chat.execute operation ct
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
