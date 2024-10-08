[<RequireQualifiedAccess>]
module EmbassyAccess.Persistence.Repository

open Infrastructure
open Persistence
open EmbassyAccess.Persistence
open Infrastructure.Logging

module Query =

    module Request =

        let get ct filter storage =
            match storage with
            | Storage.Type.InMemory context ->
                Log.trace $"InMemory query request {filter}"
                context |> InMemoryRepository.Query.Request.get ct filter
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }

        let get' ct requestId storage =
            match storage with
            | Storage.Type.InMemory context ->
                Log.trace $"InMemory query request {requestId}"
                context |> InMemoryRepository.Query.Request.get' ct requestId
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }

module Command =
    module Request =
        let execute ct command storage =
            match storage with
            | Storage.Type.InMemory context -> context |> InMemoryRepository.Command.Request.execute ct command
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }
