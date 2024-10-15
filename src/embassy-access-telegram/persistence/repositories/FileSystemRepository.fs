[<RequireQualifiedAccess>]
module internal EA.Telegram.Persistence.FileSystemRepository

open Infrastructure
open Persistence.FileSystem
open Persistence.Domain
open EA.Domain
open EA.Telegram
open EA.Telegram.Domain

module Query =
    module Chat =
        open EA.Telegram.Persistence.Query.Chat
        open EA.Telegram.Persistence.Query.Filter.Chat

        let getOne ct query storage =
            match ct |> notCanceled with
            | true ->
                let filter (data: Chat list) =
                    match query with
                    | Id id -> data |> List.tryFind (fun x -> x.Id = id)

                storage
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                |> ResultAsync.map filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

        let getMany ct query storage =
            match ct |> notCanceled with
            | true ->
                let filter (data: Chat list) =
                    match query with
                    | SearchSubscription subtId -> data |> List.filter (InMemory.hasSubscription subtId)

                storage
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                |> ResultAsync.map filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

module Command =
    module Chat =
        open EA.Telegram.Persistence.Command.Chat
        open EA.Telegram.Persistence.Command.Definitions.Chat

        let private create definition (chats: External.Chat array) =
            match definition with
            | Create.ChatSubscription(chatId, subId) ->
                match
                    chats
                    |> Array.tryFindIndex (fun x ->
                        x.Id = chatId.Value && (x.Subscriptions |> Seq.contains (subId.Value |> string)))
                with
                | Some _ ->
                    Error
                    <| Operation
                        { Message = $" Subscription {subId.Value} already exists in {chatId}."
                          Code = Some ErrorCodes.AlreadyExists }
                | None ->
                    let chat =
                        { Id = chatId
                          Subscriptions = Set.singleton subId }

                    let data = chats |> Array.append [| Mapper.Chat.toExternal chat |]
                    Ok(data, chat)

        let private createOrUpdate definition (chats: External.Chat array) =
            match definition with
            | CreateOrUpdate.ChatSubscription(chatId, subId) ->
                match
                    chats
                    |> Seq.tryFind (fun x ->
                        x.Id = chatId.Value && (x.Subscriptions |> Seq.contains (subId.Value |> string)))
                with
                | Some chat ->
                    let data = chats |> Array.mapi (fun i x -> if x.Id = chat.Id then chat else x)

                    chat |> Mapper.Chat.toInternal |> Result.map (fun chat -> (data, chat))
                | None ->
                    let chat =
                        { Id = chatId
                          Subscriptions = Set.singleton subId }

                    let data = chats |> Array.append [| Mapper.Chat.toExternal chat |]
                    Ok(data, chat)

        let private update definition (chats: External.Chat array) =
            match definition with
            | Chat chat ->
                match chats |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{chat.Id} not found to update."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    let data =
                        chats
                        |> Array.mapi (fun i x -> if i = index then Mapper.Chat.toExternal chat else x)

                    Ok(data, chat)

        let private delete definition (chats: External.Chat array) =
            match definition with
            | ChatId chatId ->
                match chats |> Array.tryFindIndex (fun x -> x.Id = chatId.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{chatId} not found to delete."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    chats[index]
                    |> Mapper.Chat.toInternal
                    |> Result.map (fun chat ->
                        let data = chats |> Array.removeAt index
                        (data, chat))

        let execute ct command storage =
            match ct |> notCanceled with
            | true ->
                storage
                |> Query.Json.get
                |> ResultAsync.bind (fun data ->
                    match command with
                    | Create definition -> data |> create definition |> Result.map id
                    | CreateOrUpdate definition -> data |> createOrUpdate definition |> Result.map id
                    | Update definition -> data |> update definition |> Result.map id
                    | Delete definition -> data |> delete definition |> Result.map id)
                |> ResultAsync.bindAsync (fun (data, item) ->
                    storage |> Command.Json.save data |> ResultAsync.map (fun _ -> item))
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return
