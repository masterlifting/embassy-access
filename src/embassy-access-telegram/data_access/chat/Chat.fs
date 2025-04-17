﻿[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Chat

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open Web.Clients.Domain.Telegram
open EA.Core.Domain
open EA.Telegram.Domain

type Storage = Provider of Storage.Provider

type Entity() =
    member val Id = 0L with get, set
    member val Subscriptions = List.empty<string> with get, set
    member val Culture = String.Empty with get, set

    member this.ToDomain() =
        this.Subscriptions
        |> Seq.map (fun x ->
            match x with
            | AP.IsUUID16 id -> Ok <| RequestId id
            | _ -> $"The subscription '{x}' is not supported." |> NotSupported |> Error)
        |> Result.choose
        |> Result.map Set.ofList
        |> Result.map (fun subscriptions -> {
            Id = this.Id |> ChatId
            Subscriptions = subscriptions
            Culture = this.Culture |> Culture.parse
        })

type private Chat with
    member private this.ToEntity() =
        let result = Entity()

        result.Id <- this.Id.Value

        result.Subscriptions <-
            this.Subscriptions
            |> Seq.map (function
                | RequestId id -> string id)
            |> Seq.toList

        result.Culture <- this.Culture.Code

        result

module internal Common =
    let create (chat: Chat) (data: Entity array) =
        match data |> Array.exists (fun x -> x.Id = chat.Id.Value) with
        | true -> $"The '{chat.Id}'" |> AlreadyExists |> Error
        | false -> data |> Array.append [| chat.ToEntity() |] |> Ok

    let update (chat: Chat) (data: Entity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
        | Some index ->
            data[index] <- chat.ToEntity()
            Ok data
        | None -> $"The '{chat.Id}' not found." |> NotFound |> Error

    let delete (chatId: ChatId) (data: Entity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = chatId.Value) with
        | Some index -> data |> Array.removeAt index |> Ok
        | None -> $"The '{chatId}' not found." |> NotFound |> Error
