[<RequireQualifiedAccess>]
module internal EA.Telegram.Persistence.InMemoryRepository

open EA.Persistence
open Infrastructure
open Persistence.InMemory
open Persistence.Domain
open EA.Domain
open EA.Telegram
open EA.Telegram.Domain

module Query =
    module Chat =
        open EA.Telegram.Persistence.Query.Chat
        open EA.Telegram.Persistence.Query.Filter.Chat

        let getOne ct query storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Chat list) =
                            match query with
                            | ById id -> data |> List.tryFind (fun x -> x.Id = id)

                        storage
                        |> Query.Json.get Key.Chats
                        |> Result.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

        let getMany ct query storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Chat list) =
                            match query with
                            | BySubscription subId -> data |> List.filter (InMemory.hasSubscription subId)
                            | BySubscriptions subIds -> data |> List.filter (InMemory.hasSubscriptions subIds)

                        storage
                        |> Query.Json.get Key.Chats
                        |> Result.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

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
                match chats |> Seq.tryFind (fun x -> x.Id = chatId.Value) with
                | Some chat ->
                    chat.Subscriptions <- chat.Subscriptions |> set |> Set.add (subId.Value |> string) |> Seq.toList
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
            | Command.Definitions.Chat.Chat chat ->
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
            | Chat chatId ->
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
            | Subscription (chatId, subId) ->
                match chats |> Array.tryFindIndex (fun x -> x.Id = chatId.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{chatId} not found to delete subscription."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    let chat = chats[index]
                    match chat.Subscriptions |> Seq.tryFindIndex (fun x -> x = (subId.Value |> string)) with
                    | None ->
                        Error
                        <| Operation
                            { Message = $"Subscription {subId.Value} not found in {chatId}."
                              Code = Some ErrorCodes.NotFound }
                    | Some subIndex ->
                        chat.Subscriptions <- chat.Subscriptions |> Seq.removeAt subIndex |> Seq.toList
                        let data = chats |> Array.mapi (fun i x -> if i = index then chat else x)
                        chat |> Mapper.Chat.toInternal |> Result.map (fun chat -> (data, chat))

        let execute command ct storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        storage
                        |> Query.Json.get Key.Chats
                        |> Result.bind (fun data ->
                            match command with
                            | Create definition -> data |> create definition |> Result.map id
                            | CreateOrUpdate definition -> data |> createOrUpdate definition |> Result.map id
                            | Update definition -> data |> update definition |> Result.map id
                            | Delete definition -> data |> delete definition |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            storage |> Command.Json.save Key.Chats data |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
