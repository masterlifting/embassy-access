[<RequireQualifiedAccess>]
module internal EA.Telegram.Persistence.FileSystemRepository

open Infrastructure
open Persistence.FileSystem
open EA.Telegram
open EA.Telegram.Domain

module Query =
    module Chat =
        open EA.Telegram.Persistence.Query.Chat

        let tryFindOne ct query client =
            match ct |> notCanceled with
            | true ->
                let filter (data: Chat list) =
                    match query with
                    | ById id -> data |> InMemory.FindOne.byId id

                client
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                |> ResultAsync.bind filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

        let findMany ct query client =
            match ct |> notCanceled with
            | true ->
                let filter (data: Chat list) =
                    match query with
                    | BySubscription subId -> data |> InMemory.FindMany.bySubscription subId
                    | BySubscriptions subIds -> data |> InMemory.FindMany.bySubscriptions subIds

                client
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                |> ResultAsync.bind filter
                |> ResultAsync.map List.ofSeq
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

module Command =
    module Chat =
        open EA.Telegram.Persistence.Command.Chat

        let execute operation ct client =
            match ct |> notCanceled with
            | true ->
                client
                |> Query.Json.get
                |> ResultAsync.bind (fun data ->
                    match operation with
                    | Create chat -> data |> InMemory.create chat |> Result.map id
                    | CreateOrUpdate chat -> data |> InMemory.createOrUpdate chat |> Result.map id
                    | Update chat -> data |> InMemory.update chat |> Result.map id
                    | Delete chatId -> data |> InMemory.delete chatId |> Result.map id)
                |> ResultAsync.bindAsync (fun (data, item) ->
                    client |> Command.Json.save data |> ResultAsync.map (fun _ -> item))
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return
