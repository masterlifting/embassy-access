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
    | Postgre of Postgre.Connection

let private toProvider =
    function
    | Chat.Storage.Provider provider -> provider

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    | InMemory connection -> connection |> Storage.Connection.InMemory |> Storage.init
    | Postgre connection ->
        {
            Database.Database = Database.Postgre connection.String
            Database.Lifetime = connection.Lifetime
        }
        |> Storage.Connection.Database
        |> Storage.init
    |> Result.map Chat.Storage.Provider

module Query =

    let tryFindById chatId storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Query.tryFindById chatId
        | Storage.FileSystem client -> client |> FileSystem.Chat.Query.tryFindById chatId
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> Postgre.Chat.Query.tryFindById chatId
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

module Command =

    let update chat storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.update chat
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.update chat
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client -> client |> Postgre.Chat.Command.update chat
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return

    let setCulture chatId culture storage =
        let provider = storage |> toProvider
        match provider with
        | Storage.InMemory client -> client |> InMemory.Chat.Command.setCulture chatId culture
        | Storage.FileSystem client -> client |> FileSystem.Chat.Command.setCulture chatId culture
        | Storage.Database database ->
            match database with
            | Database.Client.Postgre client ->
                client |> Postgre.Chat.Command.setCulture chatId culture
        | _ -> $"The '{provider}' is not supported." |> NotSupported |> Error |> async.Return
