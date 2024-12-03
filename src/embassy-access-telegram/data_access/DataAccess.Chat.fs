[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Chat

open Infrastructure
open EA.Core.Domain
open Persistence.Domain
open Web.Telegram.Domain
open EA.Telegram.Domain

[<Literal>]
let private Name = "Chats"

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

module private FileSystem =
    open Persistence.FileSystem

    let private loadData = Query.Json.get<Chat>

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> ResultAsync.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)

let initialize storageType =
    match storageType with
    | FileSystem filePath ->
        { Persistence.Domain.FileSystem.FilePath = filePath
          Persistence.Domain.FileSystem.FileName = Name }
        |> Connection.FileSystem
        |> Persistence.Storage.create
    | InMemory -> Connection.InMemory |> Persistence.Storage.create

let tryFindById chatId storage =
    match storage with
    | Storage.InMemory client -> client |> InMemory.tryFindById chatId
    | Storage.FileSystem client -> client |> FileSystem.tryFindById chatId
    | _ -> $"Storage {storage}" |> NotSupported |> Error |> async.Return
