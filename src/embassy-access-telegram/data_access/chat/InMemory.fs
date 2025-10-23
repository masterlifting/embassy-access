module EA.Telegram.DataAccess.InMemory.Chat

open System
open Infrastructure.Domain
open Web.Clients.Domain.Telegram
open EA.Telegram.DataAccess
open EA.Telegram.Domain
open Persistence.Storages.InMemory

let private loadData = Query.Json.get<Chat.Entity>

module Query =

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> Result.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> Result.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)
        |> async.Return

module Command =

    let update chat client =
        client
        |> loadData
        |> Result.bind (Chat.Common.update chat)
        |> Result.bind (fun data -> client |> Command.Json.save data)
        |> Result.map (fun _ -> chat)
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
