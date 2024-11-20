[<RequireQualifiedAccess>]
module internal EA.Telegram.Persistence.InMemoryRepository

open EA.Persistence
open Infrastructure
open Persistence.InMemory
open Persistence.Domain
open EA.Core.Domain
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
                        |> Query.Json.get Key.CHATS_STORAGE_NAME
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
                        |> Query.Json.get Key.CHATS_STORAGE_NAME
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
        open EA.Telegram.Persistence.Command.Chat.InMemory

        let execute operation ct client =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        client
                        |> Query.Json.get Key.CHATS_STORAGE_NAME
                        |> Result.bind (fun data ->
                            match operation with
                            | Create chat -> data |> create chat |> Result.map id
                            | CreateOrUpdate chat -> data |> createOrUpdate chat |> Result.map id
                            | Update chat -> data |> update chat |> Result.map id
                            | Delete chatId -> data |> delete chatId |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            client
                            |> Command.Json.save Key.CHATS_STORAGE_NAME data
                            |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
