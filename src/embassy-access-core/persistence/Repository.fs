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
                Log.trace $"InMemory get request with filter \n{filter}"
                context |> InMemoryRepository.Query.Request.get ct filter
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }

        let get' ct requestId storage =
            match storage with
            | Storage.Type.InMemory context ->
                Log.trace $"InMemory get request with id {requestId}"
                context |> InMemoryRepository.Query.Request.get' ct requestId
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }

    module Telegram =

        module Chat =

            let get ct filter storage =
                match storage with
                | Storage.Type.InMemory context ->
                    Log.trace $"InMemory Telegram.Chat get request with filter \n{filter}"
                    context |> InMemoryRepository.Query.Telegram.Chat.get ct filter
                | _ -> async { return Error <| NotSupported $"Storage {storage}" }

module Command =

    module Request =

        let private execute ct command storage =
            match storage with
            | Storage.Type.InMemory context ->
                Log.trace $"InMemory command request {command}"
                context |> InMemoryRepository.Command.Request.execute ct command
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }

        let create ct request =
            Command.Request.Create request |> execute ct

        let update ct request =
            Command.Request.Update request |> execute ct

        let delete ct request =
            Command.Request.Delete request |> execute ct
