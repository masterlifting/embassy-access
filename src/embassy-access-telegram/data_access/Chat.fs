[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Chat

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Core.Domain
open Persistence
open Web.Telegram.Domain
open EA.Telegram.Domain

[<Literal>]
let private Name = "Chats"

type ChatStorage = ChatStorage of Storage.Type

type StorageType =
    | InMemory
    | FileSystem of filepath: string

type internal ChatEntity() =
    member val Id = 0L with get, set
    member val Subscriptions = List.empty<string> with get, set

    member this.ToDomain() =
        this.Subscriptions
        |> Seq.map (fun x ->
            match x with
            | AP.IsGuid id -> Ok <| RequestId id
            | _ -> $"Chat subscription {x}" |> NotSupported |> Error)
        |> Result.choose
        |> Result.map Set.ofList
        |> Result.map (fun subscriptions ->
            { Id = this.Id |> ChatId
              Subscriptions = subscriptions })

type private Chat with
    member private this.ToEntity() =
        let result = ChatEntity()

        result.Id <- this.Id.Value

        result.Subscriptions <-
            this.Subscriptions
            |> Seq.map (function
                | RequestId id -> string id)
            |> Seq.toList

        result

module private Common =
    let create (chat: Chat) (data: ChatEntity array) =
        match data |> Array.exists (fun x -> x.Id = chat.Id.Value) with
        | true -> $"{chat.Id}" |> AlreadyExists |> Error
        | false -> data |> Array.append [| chat.ToEntity() |] |> Ok

    let update (chat: Chat) (data: ChatEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
        | Some index ->
            data[index] <- chat.ToEntity()
            Ok data
        | None -> $"{chat.Id}" |> NotFound |> Error

module private InMemory =
    open Persistence.InMemory

    let private loadData = Query.Json.get<ChatEntity> Name

    let create chat client =
        client
        |> loadData
        |> Result.bind (Common.create chat)
        |> Result.bind (fun data -> client |> Command.Json.save Name data)
        |> async.Return

    let update chat client =
        client
        |> loadData
        |> Result.bind (Common.update chat)
        |> Result.bind (fun data -> client |> Command.Json.save Name data)
        |> async.Return

    let createOrUpdate chat client =
        client
        |> loadData
        |> Result.bind (fun data ->
            match data |> Seq.exists (fun x -> x.Id = chat.Id.Value) with
            | true -> data |> Common.update chat
            | false -> data |> Common.create chat)
        |> Result.bind (fun data -> client |> Command.Json.save Name data)
        |> async.Return

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> Result.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> Result.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)
        |> async.Return

module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<ChatEntity>

    let create chat client =
        client
        |> loadData
        |> ResultAsync.bind (Common.create chat)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let update chat client =
        client
        |> loadData
        |> ResultAsync.bind (Common.update chat)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let createOrUpdate chat client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.exists (fun x -> x.Id = chat.Id.Value) with
            | true -> data |> Common.update chat
            | false -> data |> Common.create chat)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> ResultAsync.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)

let private toPersistenceStorage storage =
    storage
    |> function
        | ChatStorage storage -> storage

let init storageType =
    match storageType with
    | FileSystem filePath ->
        { Persistence.FileSystem.Domain.Source.FilePath = filePath
          Persistence.FileSystem.Domain.Source.FileName = Name }
        |> Storage.Connection.FileSystem
        |> Storage.init
    | InMemory -> Storage.Connection.InMemory |> Storage.init
    |> Result.map ChatStorage

module Query =
    let tryFindById chatId storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.tryFindById chatId
        | Storage.FileSystem client -> client |> FileSystem.tryFindById chatId
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

module Command =
    let create chat storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.create chat
        | Storage.FileSystem client -> client |> FileSystem.create chat
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let update chat storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.update chat
        | Storage.FileSystem client -> client |> FileSystem.update chat
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return

    let createOrUpdate chat storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.createOrUpdate chat
        | Storage.FileSystem client -> client |> FileSystem.createOrUpdate chat
        | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
