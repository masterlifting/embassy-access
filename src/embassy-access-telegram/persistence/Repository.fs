[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Repository

open Infrastructure
open Infrastructure.Logging
open Persistence.Domain

module Query =
    module Chat =
        Log.trace $"InMemory query request {query}"

        let getOne query ct storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Chat.getOne ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Chat.getOne ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany query ct storageType =
            Log.trace $"InMemory query request {query}"

            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Chat.getMany ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Chat.getMany ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

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
            |> ResultAsync.map (Seq.map _.Embassy)

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
                    |> ResultAsync.map (Seq.filter (fun request -> request.Embassy = embassy)))
            |> ResultAsync.map List.ofSeq

module Command =
    module Chat =
        let execute command ct storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Chat.execute command ct
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Command.Chat.execute command ct
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let createOrUpdateSubscription (chatId, requestId) ct storage =
            let command =
                (chatId, requestId)
                |> Command.Definitions.Chat.CreateOrUpdate.ChatSubscription
                |> Command.Chat.CreateOrUpdate

            storage |> execute command ct

    module Request =
        open EA.Domain
        open EA.Persistence.Command.Definitions.Request

        let createOrUpdatePassportSearch (embassy, payload) ct storage =
            let commandDefinition: PassportsGroup =
                { Embassy = embassy
                  Payload = payload
                  ConfirmationState = Disabled
                  Validation = Some EA.Api.validateRequest }

            let command =
                commandDefinition
                |> EA.Persistence.Command.Definitions.Request.CreateOrUpdate.PassportsGroup
                |> EA.Persistence.Command.Request.CreateOrUpdate

            storage |> EA.Persistence.Repository.Command.Request.execute command ct

        let createOrUpdateOthersSearch (embassy, payload) ct storage =
            let commandDefinition: OthersGroup =
                { Embassy = embassy
                  Payload = payload
                  ConfirmationState = Disabled
                  Validation = Some EA.Api.validateRequest }

            let command =
                commandDefinition
                |> EA.Persistence.Command.Definitions.Request.CreateOrUpdate.OthersGroup
                |> EA.Persistence.Command.Request.CreateOrUpdate

            storage |> EA.Persistence.Repository.Command.Request.execute command ct

        let createOrUpdatePassportResultSearch (embassy, payload) ct storage =
            let commandDefinition: PassportResultGroup =
                { Embassy = embassy
                  Payload = payload
                  Validation = Some EA.Api.validateRequest }

            let command =
                commandDefinition
                |> EA.Persistence.Command.Definitions.Request.CreateOrUpdate.PassportResultGroup
                |> EA.Persistence.Command.Request.CreateOrUpdate

            storage |> EA.Persistence.Repository.Command.Request.execute command ct
