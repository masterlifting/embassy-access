module EA.Telegram.DataAccess.FileSystem.Chat

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Domain
open Persistence.Storages.FileSystem

let private loadData = Query.Json.get<Chat.Entity>

module Query =

    let getSubscriptions client =
        client
        |> loadData
        |> ResultAsync.map (Seq.collect _.Subscriptions)
        |> ResultAsync.map (Seq.map RequestId.parse)
        |> ResultAsync.bind Result.choose

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> ResultAsync.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)

    let findManyBySubscription (subscriptionId: RequestId) client =
        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun x -> x.Subscriptions |> Seq.exists (fun y -> y = subscriptionId.ValueStr)))
        |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

    let findManyBySubscriptions (subscriptionIds: RequestId seq) client =
        let subscriptionIds = subscriptionIds |> Seq.map _.ValueStr |> Set.ofSeq

        client
        |> loadData
        |> ResultAsync.map (Seq.filter (fun x -> x.Subscriptions |> Seq.exists subscriptionIds.Contains))
        |> ResultAsync.bind (Seq.map _.ToDomain() >> Result.choose)

module Command =
    let create chat client =
        client
        |> loadData
        |> ResultAsync.bind (Chat.Common.create chat)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> chat)

    let update chat client =
        client
        |> loadData
        |> ResultAsync.bind (Chat.Common.update chat)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> chat)

    let createChatSubscription (chatId: ChatId) (subscription: RequestId) client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
            | None -> $"The '{chatId}' not found." |> NotFound |> Error
            | Some index ->
                data[index].Subscriptions <-
                    data[index].Subscriptions |> Set |> Set.add subscription.ValueStr |> Seq.toList

                data |> Ok)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let deleteChatSubscription (chatId: ChatId) (subscription: RequestId) client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
            | None -> $"The '{chatId}' not found." |> NotFound |> Error
            | Some index ->
                data[index].Subscriptions <-
                    data[index].Subscriptions
                    |> List.filter (fun subValue -> subValue <> subscription.ValueStr)

                data |> Ok)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let deleteChatSubscriptions (chatId: ChatId) (subscriptions: RequestId Set) client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
            | None -> $"The '{chatId.ValueStr}' not found." |> NotFound |> Error
            | Some index ->
                data[index].Subscriptions <-
                    data[index].Subscriptions
                    |> List.filter (fun subValue ->
                        not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue)))

                data |> Ok)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let deleteSubscriptions (subscriptions: RequestId Set) client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            data
            |> Seq.iter (fun chat ->
                chat.Subscriptions <-
                    chat.Subscriptions
                    |> List.filter (fun subValue ->
                        not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue))))

            data |> Ok)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)

    let setCulture (chatId: ChatId) (culture: Culture) client =
        client
        |> loadData
        |> ResultAsync.bind (fun data ->
            match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
            | None ->
                data
                |> Chat.Common.create {
                    Id = chatId
                    Subscriptions = Set.empty
                    Culture = culture
                }
            | Some index ->
                data[index].Culture <- culture.Code
                data |> Ok)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
