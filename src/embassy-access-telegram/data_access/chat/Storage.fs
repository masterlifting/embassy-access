[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Storage.Chat

open Infrastructure.Domain
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type StorageType =
    | InMemory of InMemory.Connection
    | FileSystem of FileSystem.Connection

let private toProvider =
    function
    | Chat.Provider provider -> provider

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    | InMemory connection -> connection |> Storage.Connection.InMemory |> Storage.init
    |> Result.map Chat.Provider

module Query =

    let getSubscriptions storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Query.getSubscriptions
        | Storage.FileSystem client -> client |> FileSystem.Chat.Query.getSubscriptions
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let tryFindById chatId storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Query.tryFindById chatId
        | Storage.FileSystem client -> client |> FileSystem.Chat.Query.tryFindById chatId
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyBySubscription subscriptionId storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Query.findManyBySubscription subscriptionId
        | Storage.FileSystem client -> client |> FileSystem.Chat.Query.findManyBySubscription subscriptionId
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let findManyBySubscriptions subscriptionIds storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Query.findManyBySubscriptions subscriptionIds
        | Storage.FileSystem client -> client |> FileSystem.Chat.Query.findManyBySubscriptions subscriptionIds
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

module Command =
    let create chat storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.create chat
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.create chat
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let update chat storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.update chat
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.update chat
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let createChatSubscription chatId subscription storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.createChatSubscription chatId subscription
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.createChatSubscription chatId subscription
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let deleteChatSubscription chatId subscription storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.deleteChatSubscription chatId subscription
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.deleteChatSubscription chatId subscription
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let deleteChatSubscriptions chatId subscriptions storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.deleteChatSubscriptions chatId subscriptions
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.deleteChatSubscriptions chatId subscriptions
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let deleteSubscriptions subscriptions storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.deleteSubscriptions subscriptions
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.deleteSubscriptions subscriptions
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let setCulture chatId culture storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.setCulture chatId culture
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.setCulture chatId culture
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
