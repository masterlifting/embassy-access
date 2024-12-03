[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Chat

open Infrastructure
open EA.Core.Domain
open Persistence.Domain
open Web.Telegram.Domain
open EA.Telegram.Domain

[<Literal>]
let private Name = "Chats"

type ChatStorage = ChatStorage of Storage

type StorageType =
    | InMemory
    | FileSystem of filepath: string

type internal Chat() =
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
        
        member this.FromDomain(chat: EA.Telegram.Domain.Chat) =
            { Id = chat.Id.Value
              Subscriptions = chat.Subscriptions |> Set.map (fun x -> x.Value) |> List.ofSeq }

module private InMemory =
    open Persistence.InMemory

    let private loadData = Query.Json.get<Chat> Name

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> Result.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> Result.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)
        |> async.Return
        
    let createOrUpdate (chat: EA.Telegram.Domain.Chat) client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun x -> x.Id <> chat.Id.Value))
        |> Result.map (Seq.append [chat])
        |> async.Return
                                

module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<Chat>
    let create (chat: EA.Telegram.Domain.Chat) client =
        client
        |> loadData
        |> ResultAsync.map (fun data ->
            match data |> Array.exists (fun x -> x.Id = chat.Id.Value) with 
            | true ->
                Error
                <| Operation
                    { Message = $"{chat.Id} already exists."
                      Code = Some ErrorCode.ALREADY_EXISTS }
            | false ->
                
                let result = data |> Array.append [| Mapper.Chat.toExternal chat |]
                Ok(data, chat)

let update (chat: EA.Telegram.Domain.Chat) client =
    match chats |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
    | None ->
        Error
        <| Operation
            { Message = $"{chat.Id} not found to update."
              Code = Some ErrorCode.NOT_FOUND }
    | Some index ->
        let data =
            chats
            |> Array.mapi (fun i x -> if i = index then Mapper.Chat.toExternal chat else x)

        Ok(data, chat)

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> ResultAsync.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)
        
    let createOrUpdate (chat: EA.Telegram.Domain.Chat) client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun x -> x.Id <> chat.Id.Value))
        |> ResultAsync.map (Seq.append [chat])

let init storageType =
    match storageType with
    | FileSystem filePath ->
        { Persistence.Domain.FileSystem.FilePath = filePath
          Persistence.Domain.FileSystem.FileName = Name }
        |> Connection.FileSystem
        |> Persistence.Storage.create
    | InMemory -> Connection.InMemory |> Persistence.Storage.create
    |> Result.map ChatStorage

let private toPersistenceStorage storage =
    storage
    |> function
        | ChatStorage storage -> storage

let tryFindById chatId storage =
    match storage |> toPersistenceStorage with
    | Storage.InMemory client -> client |> InMemory.tryFindById chatId
    | Storage.FileSystem client -> client |> FileSystem.tryFindById chatId
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
    
let createOrUpdate chat storage =
    match storage |> toPersistenceStorage with
    | Storage.InMemory client -> client |> InMemory.createOrUpdate chat
    | Storage.FileSystem client -> client |> FileSystem.createOrUpdate chat
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
