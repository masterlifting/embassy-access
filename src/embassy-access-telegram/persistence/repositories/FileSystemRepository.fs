[<RequireQualifiedAccess>]
module internal EA.Telegram.Persistence.FileSystemRepository

open Infrastructure
open Persistence.FileSystem
open Persistence.Domain
open EA.Core.Domain
open EA.Telegram
open EA.Telegram.Domain

module Query =
    module Chat =
        open EA.Telegram.Persistence.Query.Chat
        open EA.Telegram.Persistence.Query.Filter.Chat

        let getOne ct query client =
            match ct |> notCanceled with
            | true ->
                let filter (data: Chat list) =
                    match query with
                    | ById id -> data |> List.tryFind (fun x -> x.Id = id)

                client
                |> Query.Json.get
                |> ResultAsync.bind (Seq.map Mapper.Chat.toInternal >> Result.choose)
                |> ResultAsync.map filter
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return

        let getMany ct query client =
            match ct |> notCanceled with
            | true ->
                let filter (data: Chat list) =
                    match query with
                    | BySubscription subId -> data |> List.filter (InMemory.hasSubscription subId)
                    | BySubscriptions subIds -> data |> List.filter (InMemory.hasSubscriptions subIds)

                client
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
        open EA.Telegram.Persistence.Command.Chat.InMemory

        let execute operation ct client =
            match ct |> notCanceled with
            | true ->
                client
                |> Query.Json.get
                |> ResultAsync.bind (fun data ->
                    match operation with
                    | Create chat -> data |> create chat |> Result.map id
                    | CreateOrUpdate chat -> data |> createOrUpdate chat |> Result.map id
                    | Update chat -> data |> update chat |> Result.map id
                    | Delete chatId -> data |> delete chatId |> Result.map id)
                |> ResultAsync.bindAsync (fun (data, item) ->
                    client |> Command.Json.save data |> ResultAsync.map (fun _ -> item))
            | false ->
                Error
                <| (Canceled
                    <| ErrorReason.buildLine (__SOURCE_DIRECTORY__, __SOURCE_FILE__, __LINE__))
                |> async.Return
