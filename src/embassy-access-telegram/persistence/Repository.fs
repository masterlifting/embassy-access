[<RequireQualifiedAccess>]
module EmbassyAccess.Telegram.Persistence.Repository

open Infrastructure
open Persistence
open Infrastructure.Logging

module Query =

    module Chat =

        let get ct filter storage =
            match storage with
            | Storage.Type.InMemory context ->
                Log.trace $"InMemory query request {filter}"
                context |> InMemoryRepository.Query.Chat.get ct filter
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }

module Command =

    module Chat =

        let private execute ct command storage =
            match storage with
            | Storage.Type.InMemory context ->
                Log.trace $"InMemory command request {command}"
                context |> InMemoryRepository.Command.Chat.execute ct command
            | _ -> async { return Error <| NotSupported $"Storage {storage}" }

        let create ct request =
            Command.Chat.Create request |> execute ct

        let update ct request =
            Command.Chat.Update request |> execute ct

        let delete ct request =
            Command.Chat.Delete request |> execute ct
