[<RequireQualifiedAccess>]
module internal EmbassyAccess.Telegram.Persistence.FileSystemRepository

open Infrastructure
open Persistence.FileSystem
open Persistence.Domain
open EmbassyAccess.Domain
open EmbassyAccess.Telegram
open EmbassyAccess.Telegram.Domain

module Query =
    module Chat =
        open EmbassyAccess.Telegram.Persistence.Query.Chat

        module private Filters =
            let search (requestId: RequestId) (chat: Chat) =
                chat.Subscriptions |> Set.contains requestId

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
                    | Search requestId -> data |> List.filter (Filters.search requestId)

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
            match ct |> notCanceled with
            | true ->

                storage
                |> Query.Json.get
                |> ResultAsync.bind (fun data ->
                    match operation with
                    | Create options -> data |> create options |> Result.map id
                    | Update options -> data |> update options |> Result.map id
                    | Delete options -> data |> delete options |> Result.map id)
                |> ResultAsync.bindAsync (fun (data, item) ->
                    storage |> Command.Json.save data |> ResultAsync.map (fun _ -> item))
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return
