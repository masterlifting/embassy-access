[<RequireQualifiedAccess>]
module internal EA.Telegram.Persistence.InMemoryRepository

open Infrastructure
open Persistence.InMemory
open EA.Telegram
open EA.Telegram.Domain

module Query =
    module Chat =
        open EA.Telegram.Persistence.Query.Chat

        let tryFindOne ct query storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Chat list) =
                            match query with
                            | ById id -> data |> InMemory.FindOne.byId id

                        storage
                        |> Query.Json.get Constants.CHATS_STORAGE_NAME
                        |> Result.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                        |> Result.bind filter
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

        let findMany ct query storage =
            async {
                return
                    match ct |> notCanceled with
                    | true ->
                        let filter (data: Chat list) =
                            match query with
                            | BySubscription subId -> data |> InMemory.FindMany.bySubscription subId
                            | BySubscriptions subIds -> data |> InMemory.FindMany.bySubscriptions subIds

                        storage
                        |> Query.Json.get Constants.CHATS_STORAGE_NAME
                        |> Result.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                        |> Result.bind filter
                        |> Result.map List.ofSeq
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }

module Command =
    module Chat =
        open EA.Telegram.Persistence.Command.Chat

        let execute operation ct client =
            async {
                return
                    match ct |> notCanceled with
                    | true ->

                        client
                        |> Query.Json.get Constants.CHATS_STORAGE_NAME
                        |> Result.bind (fun data ->
                            match operation with
                            | Create chat -> data |> InMemory.create chat |> Result.map id
                            | CreateOrUpdate chat -> data |> InMemory.createOrUpdate chat |> Result.map id
                            | Update chat -> data |> InMemory.update chat |> Result.map id
                            | Delete chatId -> data |> InMemory.delete chatId |> Result.map id)
                        |> Result.bind (fun (data, item) ->
                            client
                            |> Command.Json.save Constants.CHATS_STORAGE_NAME data
                            |> Result.map (fun _ -> item))
                    | false ->
                        Error
                        <| (Canceled
                            <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
            }
