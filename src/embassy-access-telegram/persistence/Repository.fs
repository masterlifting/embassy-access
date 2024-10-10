[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram.Persistence.Repository

open Infrastructure
open Persistence
open Infrastructure.Logging

module Query =
    module Chat =
        let getOne ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage ->
                Log.trace $"InMemory query request {query}"
                storage |> InMemoryRepository.Query.Chat.getOne ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage ->
                Log.trace $"InMemory query request {query}"
                storage |> InMemoryRepository.Query.Chat.getMany ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

module Command =
    module Chat =
        let execute ct operation storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Chat.execute ct operation
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return
