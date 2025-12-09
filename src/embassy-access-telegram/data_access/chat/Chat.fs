[<RequireQualifiedAccess>]
module EA.Telegram.DataAccess.Chat

open System
open Infrastructure.Domain
open Infrastructure.Prelude
open Persistence
open Web.Clients.Domain.Telegram
open EA.Telegram.Domain
open EA.Telegram.DataAccess

type Storage = Provider of Storage.Provider

type Entity() =
    member val Id = 0L with get, set
    member val Subscriptions = String.Empty with get, set
    member val Culture = String.Empty with get, set

    member this.ToDomain() =
        let subscriptionsResult =
            try
                if String.IsNullOrEmpty this.Subscriptions then
                    Ok Set.empty
                else
                    Infrastructure.SerDe.Json.deserialize<List<Subscriptions.Entity>> this.Subscriptions
                    |> Result.bind (fun entities ->
                        entities |> List.map _.ToDomain() |> Result.choose |> Result.map Set.ofList)
            with ex ->
                Error(
                    Operation {
                        Message = $"Failed to deserialize subscriptions: {ex.Message}"
                        Code = None
                    }
                )

        subscriptionsResult
        |> Result.map (fun subscriptions ->
            let chat: Chat = {
                Id = this.Id |> ChatId
                Subscriptions = subscriptions
                Culture = this.Culture |> Culture.parse
            }

            chat)

let private subscriptionToEntity (subscription: Subscription) =
    Subscriptions.Entity(
        Id = subscription.Id.Value,
        EmbassyId = subscription.EmbassyId.Value,
        ServiceId = subscription.ServiceId.Value
    )

let private chatToEntity (chat: Chat) =
    let subscriptionsJson =
        chat.Subscriptions
        |> Seq.map subscriptionToEntity
        |> Seq.toList
        |> Infrastructure.SerDe.Json.serialize
        |> Result.defaultValue "[]"

    let result = Entity(Id = chat.Id.Value, Culture = chat.Culture.Code)
    result.Subscriptions <- subscriptionsJson
    result

// Helper function for external modules to access ToEntity method
let toEntity (chat: Chat) = chatToEntity chat |> Ok

module internal Common =
    let create (chat: Chat) (data: Entity array) =
        match data |> Array.exists (fun x -> x.Id = chat.Id.Value) with
        | true -> $"The '{chat.Id}'" |> AlreadyExists |> Error
        | false -> data |> Array.append [| chatToEntity chat |] |> Ok

    let update (chat: Chat) (data: Entity array) =
        match data |> Array.tryFindIndex (fun x -> x.Id = chat.Id.Value) with
        | Some index ->
            data[index] <- chatToEntity chat
            Ok data
        | None -> $"The '{chat.Id}' not found." |> NotFound |> Error
