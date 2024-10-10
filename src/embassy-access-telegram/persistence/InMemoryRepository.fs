[<RequireQualifiedAccess>]
module internal EmbassyAccess.Telegram.Persistence.InMemoryRepository

open Infrastructure
open Persistence.InMemory
open Persistence.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Telegram
open EmbassyAccess.Telegram.Domain

[<Literal>]
let private ChatsKey = "chats"

module Query =
    module Chat =
        open EmbassyAccess.Telegram.Persistence.Query.Chat

        module private Filters =
            let search (requestId: RequestId) (chat: Chat) =
                chat.Subscriptions |> Set.contains requestId

        let getOne ct query storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Chat list) =
                            match query with
                            | Id id -> data |> List.tryFind (fun x -> x.Id = id)

                        storage
                        |> Query.Json.get ChatsKey
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
                            | Search requestId -> data |> List.filter (Filters.search requestId)

                        storage
                        |> Query.Json.get ChatsKey
                        |> Result.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                        |> Result.map filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

module Command =
    module Chat =
        open EmbassyAccess.Telegram.Persistence.Command.Chat
        open EmbassyAccess.Telegram.Persistence.Command.Options.Chat

        let private create create (data: External.Chat array) =
            match create with
            | ByRequestId(chatId, requestId) ->
                match data |> Array.tryFindIndex (fun x -> x.Id = chatId.Value) with
                | Some _ ->
                    Error
                    <| Operation
                        { Message = $"{chatId} already exists."
                          Code = Some ErrorCodes.AlreadyExists }
                | None ->
                    let chat =
                        { Id = chatId
                          Subscriptions = Set.singleton requestId }

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
                        |> Query.Json.get ChatsKey
                        |> Result.bind (fun data ->
                            match operation with
                            | Create options -> data |> create options |> Result.map id
                            | Update options -> data |> update options |> Result.map id
                            | Delete options -> data |> delete options |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            storage |> Command.Json.save ChatsKey data |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
