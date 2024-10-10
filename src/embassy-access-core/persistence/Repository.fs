[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Repository

open Infrastructure
open Persistence
open EmbassyAccess.Persistence
open Infrastructure.Logging

module Query =
    module Request =
        let getOne ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage ->
                Log.trace $"InMemory query request {query}"
                storage |> InMemoryRepository.Query.Request.getOne ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage ->
                Log.trace $"InMemory query request {query}"
                storage |> InMemoryRepository.Query.Request.getMany ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

module Command =
    module Request =
        let execute ct operation storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Request.execute ct operation
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return
