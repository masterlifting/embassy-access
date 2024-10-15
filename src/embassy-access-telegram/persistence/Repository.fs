[<RequireQualifiedAccess>]
module EA.Telegram.Persistence.Repository

open Infrastructure
open Infrastructure.Logging
open Persistence.Domain

module Query =
    module Chat =
        Log.trace $"InMemory query request {query}"

        let getOne ct query storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Chat.getOne ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Chat.getOne ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let getMany ct query storageType =
            Log.trace $"InMemory query request {query}"

            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Query.Chat.getMany ct query
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Query.Chat.getMany ct query
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

module Command =
    module Chat =
        let execute ct command storageType =
            match storageType with
            | Storage.Type.InMemory storage -> storage |> InMemoryRepository.Command.Chat.execute ct command
            | Storage.Type.FileSystem storage -> storage |> FileSystemRepository.Command.Chat.execute ct command
            | _ -> $"Storage {storageType}" |> NotSupported |> Error |> async.Return

        let createOrUpdateSubscription ct (chatId, requestId) storage =
            let command =
                (chatId, requestId)
                |> Command.Definitions.Chat.CreateOrUpdate.ChatSubscription
                |> Command.Chat.CreateOrUpdate

            storage |> execute ct command

    module Request =
        open EA.Domain
        open EA.Persistence.Command.Definitions.Request

        let createOrUpdatePassportSearch ct (embassy, payload) storage =
            let commandDefinition: PassportsGroup =
                { Embassy = embassy
                  Payload = payload
                  ConfirmationState = Disabled
                  Validation = Some EA.Api.validateRequest }

            let command =
                commandDefinition
                |> EA.Persistence.Command.Definitions.Request.CreateOrUpdate.PassportsGroup
                |> EA.Persistence.Command.Request.CreateOrUpdate

            storage |> EA.Persistence.Repository.Command.Request.execute ct command
