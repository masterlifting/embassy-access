[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Chat

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open Persistence.Storages
open Persistence.Storages.Domain
open Web.Clients.Domain.Telegram
open EA.Core.Domain
open EA.Telegram.Domain

[<Literal>]
let private Name = "Chats"

type ChatStorage = ChatStorage of Storage.Provider

type StorageType =
    | InMemory
    | FileSystem of FileSystem.Connection

type ChatEntity() =
    member val Id = 0L with get, set
    member val Subscriptions = List.empty<string> with get, set
    member val Culture = String.Empty with get, set

    member this.ToDomain() =
        this.Subscriptions
        |> Seq.map (fun x ->
            match x with
            | AP.IsUUID16 id -> Ok <| RequestId id
            | _ -> $"The subscription '{x}'" |> NotSupported |> Error)
        |> Result.choose
        |> Result.map Set.ofList
        |> Result.map (fun subscriptions ->
            { Id = this.Id |> ChatId
              Subscriptions = subscriptions
              Culture = this.Culture |> Culture.parse })

type private Chat with
    member private this.ToEntity() =
        let result = ChatEntity()

        result.Id <- this.Id.Value

        result.Subscriptions <-
            this.Subscriptions
            |> Seq.map (function
                | RequestId id -> string id)
            |> Seq.toList

        result.Culture <- this.Culture.Code

        result

module private Common =
    let create (chat: Chat) (data: ChatEntity array) =
        match data |> Array.exists (fun x -> x.Id = chat.Id.Value) with
        | true -> $"The '{chat.Id}'" |> AlreadyExists |> Error
        | false -> data |> Array.append [| chat.ToEntity() |] |> Ok

    let update (chat: Chat) (data: ChatEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
        | Some index ->
            data[index] <- chat.ToEntity()
            Ok data
        | None -> $"The '{chat.Id}'" |> NotFound |> Error

    let delete (chatId: ChatId) (data: ChatEntity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = chatId.Value) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"The '{chatId}'" |> NotFound |> Error

module private InMemory =
    open Persistence.Storages.InMemory

    let private loadData = Query.Json.get<ChatEntity> Name

    module Query =

        let getSubscriptions client =
            client
            |> loadData
            |> Result.map (Seq.collect _.Subscriptions)
            |> Result.map (Seq.map RequestId.parse)
            |> Result.bind Result.choose
            |> async.Return

        let tryFindById (id: ChatId) client =
            client
            |> loadData
            |> Result.map (Seq.tryFind (fun x -> x.Id = id.Value))
            |> Result.bind (function
                | None -> None |> Ok
                | Some chat -> chat.ToDomain() |> Result.map Some)
            |> async.Return

        let findManyBySubscription (subscriptionId: RequestId) client =
            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> x.Subscriptions |> Seq.exists (fun y -> y = subscriptionId.ValueStr)))
            |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
            |> async.Return

        let findManyBySubscriptions (subscriptionIds: RequestId seq) client =
            let subscriptionIds = subscriptionIds |> Seq.map _.ValueStr |> Set.ofSeq

            client
            |> loadData
            |> Result.map (Seq.filter (fun x -> x.Subscriptions |> Seq.exists subscriptionIds.Contains))
            |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
            |> async.Return

    module Command =
        let create chat client =
            client
            |> loadData
            |> Result.bind (Common.create chat)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> Result.map (fun _ -> chat)
            |> async.Return

        let update chat client =
            client
            |> loadData
            |> Result.bind (Common.update chat)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> Result.map (fun _ -> chat)
            |> async.Return

        let createChatSubscription (chatId: ChatId) (subscription: RequestId) client =
            client
            |> loadData
            |> Result.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None -> $"The '{chatId}'" |> NotFound |> Error
                | Some index ->
                    data[index].Subscriptions <-
                        data[index].Subscriptions |> Set |> Set.add subscription.ValueStr |> Seq.toList

                    data |> Ok)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> async.Return

        let deleteChatSubscription (chatId: ChatId) (subscription: RequestId) client =
            client
            |> loadData
            |> Result.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None -> $"The '{chatId}'" |> NotFound |> Error
                | Some index ->
                    data[index].Subscriptions <-
                        data[index].Subscriptions
                        |> List.filter (fun subValue -> subValue <> subscription.ValueStr)

                    data |> Ok)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> async.Return

        let deleteChatSubscriptions (chatId: ChatId) (subscriptions: RequestId Set) client =
            client
            |> loadData
            |> Result.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None -> $"The '{chatId.ValueStr}'" |> NotFound |> Error
                | Some index ->
                    data[index].Subscriptions <-
                        data[index].Subscriptions
                        |> List.filter (fun subValue ->
                            not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue)))

                    data |> Ok)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> async.Return

        let deleteSubscriptions (subscriptions: RequestId Set) client =
            client
            |> loadData
            |> Result.bind (fun data ->
                data
                |> Seq.iter (fun chat ->
                    chat.Subscriptions <-
                        chat.Subscriptions
                        |> List.filter (fun subValue ->
                            not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue))))

                data |> Ok)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> async.Return

        let setCulture (chatId: ChatId) (culture: Culture) client =
            client
            |> loadData
            |> Result.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None ->
                    data
                    |> Common.create
                        { Id = chatId
                          Subscriptions = Set.empty
                          Culture = culture }
                | Some index ->
                    data[index].Culture <- culture.Code
                    data |> Ok)
            |> Result.bind (fun data -> client |> Command.Json.save Name data)
            |> async.Return

module private FileSystem =
    open Persistence.Storages.FileSystem

    let private loadData = Query.Json.get<ChatEntity>

    module Query =

        let getSubscriptions client =
            client
            |> loadData
            |> ResultAsync.map (Seq.collect _.Subscriptions)
            |> ResultAsync.map (Seq.map RequestId.parse)
            |> ResultAsync.bind Result.choose

        let tryFindById (id: ChatId) client =
            client
            |> loadData
            |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.Value))
            |> ResultAsync.bind (function
                | None -> None |> Ok
                | Some chat -> chat.ToDomain() |> Result.map Some)

        let findManyBySubscription (subscriptionId: RequestId) client =
            client
            |> loadData
            |> ResultAsync.map (
                Seq.filter (fun x -> x.Subscriptions |> Seq.exists (fun y -> y = subscriptionId.ValueStr))
            )
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

        let findManyBySubscriptions (subscriptionIds: RequestId seq) client =
            let subscriptionIds = subscriptionIds |> Seq.map _.ValueStr |> Set.ofSeq

            client
            |> loadData
            |> ResultAsync.map (Seq.filter (fun x -> x.Subscriptions |> Seq.exists subscriptionIds.Contains))
            |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

    module Command =
        let create chat client =
            client
            |> loadData
            |> ResultAsync.bind (Common.create chat)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> chat)

        let update chat client =
            client
            |> loadData
            |> ResultAsync.bind (Common.update chat)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
            |> ResultAsync.map (fun _ -> chat)

        let createChatSubscription (chatId: ChatId) (subscription: RequestId) client =
            client
            |> loadData
            |> ResultAsync.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None -> $"The '{chatId}'" |> NotFound |> Error
                | Some index ->
                    data[index].Subscriptions <-
                        data[index].Subscriptions |> Set |> Set.add subscription.ValueStr |> Seq.toList

                    data |> Ok)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

        let deleteChatSubscription (chatId: ChatId) (subscription: RequestId) client =
            client
            |> loadData
            |> ResultAsync.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None -> $"The '{chatId}'" |> NotFound |> Error
                | Some index ->
                    data[index].Subscriptions <-
                        data[index].Subscriptions
                        |> List.filter (fun subValue -> subValue <> subscription.ValueStr)

                    data |> Ok)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

        let deleteChatSubscriptions (chatId: ChatId) (subscriptions: RequestId Set) client =
            client
            |> loadData
            |> ResultAsync.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None -> $"The '{chatId}'" |> NotFound |> Error
                | Some index ->
                    data[index].Subscriptions <-
                        data[index].Subscriptions
                        |> List.filter (fun subValue ->
                            not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue)))

                    data |> Ok)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

        let deleteSubscriptions (subscriptions: RequestId Set) client =
            client
            |> loadData
            |> ResultAsync.bind (fun data ->
                data
                |> Seq.iter (fun chat ->
                    chat.Subscriptions <-
                        chat.Subscriptions
                        |> List.filter (fun subValue ->
                            not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue))))

                data |> Ok)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

        let setCulture (chatId: ChatId) (culture: Culture) client =
            client
            |> loadData
            |> ResultAsync.bind (fun data ->
                match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
                | None ->
                    data
                    |> Common.create
                        { Id = chatId
                          Subscriptions = Set.empty
                          Culture = culture }
                | Some index ->
                    data[index].Culture <- culture.Code
                    data |> Ok)
            |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)


let private toPersistenceStorage storage =
    storage
    |> function
        | ChatStorage storage -> storage

let init storageType =
    match storageType with
    | FileSystem connection -> connection |> Storage.Connection.FileSystem |> Storage.init
    | InMemory -> Storage.Connection.InMemory |> Storage.init
    |> Result.map ChatStorage

module Query =

    let getSubscriptions storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.getSubscriptions
        | Storage.FileSystem client -> client |> FileSystem.Query.getSubscriptions
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let tryFindById chatId storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.tryFindById chatId
        | Storage.FileSystem client -> client |> FileSystem.Query.tryFindById chatId
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let findManyBySubscription subscriptionId storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyBySubscription subscriptionId
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyBySubscription subscriptionId
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let findManyBySubscriptions subscriptionIds storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Query.findManyBySubscriptions subscriptionIds
        | Storage.FileSystem client -> client |> FileSystem.Query.findManyBySubscriptions subscriptionIds
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

module Command =
    let create chat storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.create chat
        | Storage.FileSystem client -> client |> FileSystem.Command.create chat
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let update chat storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.update chat
        | Storage.FileSystem client -> client |> FileSystem.Command.update chat
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let createChatSubscription chatId subscription storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.createChatSubscription chatId subscription
        | Storage.FileSystem client -> client |> FileSystem.Command.createChatSubscription chatId subscription
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let deleteChatSubscription chatId subscription storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.deleteChatSubscription chatId subscription
        | Storage.FileSystem client -> client |> FileSystem.Command.deleteChatSubscription chatId subscription
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let deleteChatSubscriptions chatId subscriptions storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.deleteChatSubscriptions chatId subscriptions
        | Storage.FileSystem client -> client |> FileSystem.Command.deleteChatSubscriptions chatId subscriptions
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let deleteSubscriptions subscriptions storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.deleteSubscriptions subscriptions
        | Storage.FileSystem client -> client |> FileSystem.Command.deleteSubscriptions subscriptions
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return

    let setCulture chatId culture storage =
        match storage |> toPersistenceStorage with
        | Storage.InMemory client -> client |> InMemory.Command.setCulture chatId culture
        | Storage.FileSystem client -> client |> FileSystem.Command.setCulture chatId culture
        | _ -> $"The '{storage}'" |> NotSupported |> Error |> async.Return
