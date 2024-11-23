[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Repository

open Infrastructure
open Infrastructure.Logging
open Persistence.Domain

module Query =
    module Chat =
        Log.trace $"InMemory query request {query}"

        let getOne query ct storage =
            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Chat.getOne ct query
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Chat.getOne ct query
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

        let getMany query ct storage =
            Log.trace $"InMemory query request {query}"

            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Query.Chat.getMany ct query
            | Storage.FileSystem client -> client |> FileSystemRepository.Query.Chat.getMany ct query
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

        let tryGetOne chatId ct storage =
            let query = Query.Chat.GetOne.ById chatId
            storage |> getOne query ct

        let getManyBySubscription requestId ct storage =
            let query = Query.Chat.GetMany.BySubscription requestId
            storage |> getMany query ct

        let getManyBySubscriptions requestIds ct storage =
            let query = Query.Chat.GetMany.BySubscriptions requestIds
            storage |> getMany query ct

        let getChatRequests (chat: EA.Telegram.Domain.Chat) ct storage =
            let query = EA.Persistence.Query.Request.GetMany.ByIds chat.Subscriptions
            storage |> EA.Persistence.Repository.Query.Request.getMany query ct

        let getChatEmbassies (chat: EA.Telegram.Domain.Chat) ct storage =
            let query = EA.Persistence.Query.Request.GetMany.ByIds chat.Subscriptions

            storage
            |> EA.Persistence.Repository.Query.Request.getMany query ct
            |> ResultAsync.map (Seq.map _.Service.Embassy)

        let getEmbassyRequests embassy ct storage =
            let query = EA.Persistence.Query.Request.GetMany.ByEmbassy embassy
            storage |> EA.Persistence.Repository.Query.Request.getMany query ct

        let getChatEmbassyRequests (chatId: Web.Telegram.Domain.ChatId) embassy ct storage =
            storage
            |> tryGetOne chatId ct
            |> ResultAsync.bindAsync (function
                | None -> Seq.empty |> Ok |> async.Return
                | Some chat ->
                    storage
                    |> getChatRequests chat ct
                    |> ResultAsync.map (Seq.filter (fun request -> request.Service.Embassy = embassy)))
            |> ResultAsync.map List.ofSeq

module Command =
    module Chat =
        let execute command ct storage =
            match storage with
            | Storage.InMemory client -> client |> InMemoryRepository.Command.Chat.execute command ct
            | Storage.FileSystem client -> client |> FileSystemRepository.Command.Chat.execute command ct
            | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
