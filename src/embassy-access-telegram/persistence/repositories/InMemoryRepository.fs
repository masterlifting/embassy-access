[<RequireQualifiedAccess>]
module internal EA.Telegram.Persistence.InMemoryRepository

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
                            | Id id -> data |> List.tryFind (fun x -> x.Id = id)

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
                            | SearchSubscription subId -> data |> List.filter (InMemory.hasSubscription subId)

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
        open EA.Telegram.Persistence.Command.Options.Chat

        let private create create (data: External.Chat array) =
            match create with
            | ChatSubscription(chatId, subId) ->
                match
                    data
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

                    let data = data |> Array.append [| Mapper.Chat.toExternal chat |]
                    Ok(data, chat)

        let private update update (data: External.Chat array) =
            match update with
            | Chat chat ->
                match data |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{chat.Id} not found to update."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    let data =
                        data
                        |> Array.mapi (fun i x -> if i = index then Mapper.Chat.toExternal chat else x)

                    Ok(data, chat)

        let private delete delete (data: External.Chat array) =
            match delete with
            | ChatId chatId ->
                match data |> Array.tryFindIndex (fun x -> x.Id = chatId.Value) with
                | None ->
                    Error
                    <| Operation
                        { Message = $"{chatId} not found to delete."
                          Code = Some ErrorCodes.NotFound }
                | Some index ->
                    data[index]
                    |> Mapper.Chat.toInternal
                    |> Result.map (fun chat ->
                        let data = data |> Array.removeAt index
                        (data, chat))

        let execute ct operation storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        storage
                        |> Query.Json.get Key.Chats
                        |> Result.bind (fun data ->
                            match operation with
                            | Create options -> data |> create options |> Result.map id
                            | Update options -> data |> update options |> Result.map id
                            | Delete options -> data |> delete options |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            storage |> Command.Json.save Key.Chats data |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
