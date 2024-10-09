[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Repository

open Infrastructure
open Persistence
open EmbassyAccess.Persistence
open Infrastructure.Logging

module Request =
    module Query =
        let getOne ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage ->
                Log.trace $"InMemory query request {query}"
                storage |> InMemoryRepository.Request.Query.getOne ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage ->
                Log.trace $"InMemory query request {query}"
                storage |> InMemoryRepository.Request.Query.getMany ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

    module Command =
        let execute ct operation storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Request.Command.execute ct operation
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return
