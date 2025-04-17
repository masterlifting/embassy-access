module EA.Telegram.DataAccess.InMemory.Chat

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open EA.Core.Domain
open EA.Telegram.DataAccess
open EA.Telegram.Domain
open Persistence.Storages.InMemory

let private loadData = Query.Json.get<Chat.Entity>

module Query =

    let getSubscriptions client =
        client
        |> loadData
        |> Result.map (Seq.collect _.Subscriptions)
        |> Result.map (Seq.map RequestId.parse)
        |> Result.bind Result.choose
        |> async.Return

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> Result.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> Result.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)
        |> async.Return

    let findManyBySubscription (subscriptionId: RequestId) client =
        client
        |> loadData
        |> Result.map (Seq.filter (fun x -> x.Subscriptions |> Seq.exists (fun y -> y = subscriptionId.ValueStr)))
        |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
        |> async.Return

    let findManyBySubscriptions (subscriptionIds: RequestId seq) client =
        let subscriptionIds = subscriptionIds |> Seq.map _.ValueStr |> Set.ofSeq

        client
        |> loadData
        |> Result.map (Seq.filter (fun x -> x.Subscriptions |> Seq.exists subscriptionIds.Contains))
        |> Result.bind (Seq.map _.ToDomain() >> Result.choose)
        |> async.Return

module Command =
    let create chat client =
        client
        |> loadData
        |> Result.bind (Chat.Common.create chat)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> Result.map (fun _ -> chat)
        |> async.Return

    let update chat client =
        client
        |> loadData
        |> Result.bind (Chat.Common.update chat)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> Result.map (fun _ -> chat)
        |> async.Return

    let createChatSubscription (chatId: ChatId) (subscription: RequestId) client =
        client
        |> loadData
        |> Result.bind (fun data ->
            match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
            | None -> $"The '{chatId}' not found." |> NotFound |> Error
            | Some index ->
                data[index].Subscriptions <-
                    data[index].Subscriptions |> Set |> Set.add subscription.ValueStr |> Seq.toList

                data |> Ok)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> async.Return

    let deleteChatSubscription (chatId: ChatId) (subscription: RequestId) client =
        client
        |> loadData
        |> Result.bind (fun data ->
            match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
            | None -> $"The '{chatId}' not found." |> NotFound |> Error
            | Some index ->
                data[index].Subscriptions <-
                    data[index].Subscriptions
                    |> List.filter (fun subValue -> subValue <> subscription.ValueStr)

                data |> Ok)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> async.Return

    let deleteChatSubscriptions (chatId: ChatId) (subscriptions: RequestId Set) client =
        client
        |> loadData
        |> Result.bind (fun data ->
            match data |> Seq.tryFindIndex (fun chat -> chat.Id = chatId.Value) with
            | None -> $"The '{chatId.ValueStr}' not found." |> NotFound |> Error
            | Some index ->
                data[index].Subscriptions <-
                    data[index].Subscriptions
                    |> List.filter (fun subValue ->
                        not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue)))

                data |> Ok)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> async.Return

    let deleteSubscriptions (subscriptions: RequestId Set) client =
        client
        |> loadData
        |> Result.bind (fun data ->
            data
            |> Seq.iter (fun chat ->
                chat.Subscriptions <-
                    chat.Subscriptions
                    |> List.filter (fun subValue ->
                        not (subscriptions |> Set.exists (fun sub -> sub.ValueStr = subValue))))

            data |> Ok)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> async.Return

    let setCulture (chatId: ChatId) (culture: Culture) client =
        client
        |> loadData
        |> Result.bind (fun data ->
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
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> async.Return
