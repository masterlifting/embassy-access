module EA.Telegram.DataAccess.FileSystem.Chat

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open Persistence.Storages.FileSystem

let private loadData = Query.Json.get<Chat.Entity>

module Query =

    let tryFindById (id: ChatId) client =
        client
        |> loadData
        |> ResultAsync.map (Seq.tryFind (fun x -> x.Id = id.Value))
        |> ResultAsync.bind (function
            | None -> None |> Ok
            | Some chat -> chat.ToDomain() |> Result.map Some)

module Command =
    let update chat client =
        client
        |> loadData
        |> ResultAsync.bind (Chat.Common.update chat)
        |> ResultAsync.bindAsync (fun data -> client |> Command.Json.save data)
        |> ResultAsync.map (fun _ -> chat)

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
