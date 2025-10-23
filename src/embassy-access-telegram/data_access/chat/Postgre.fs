module EA.Telegram.DataAccess.Postgre.Chat

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open EA.Core.Domain
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open Persistence.Storages.Postgre
open Persistence.Storages.Domain.Postgre

module Query =

    let getSubscriptions (client: Client) =
        async {
            let request = {
                Sql = "SELECT id, subscriptions, culture FROM chats"
                Params = None
            }

            return!
                client
                |> Query.get<Chat.Entity> request
                |> ResultAsync.bind (fun entities ->
                    entities
                    |> Seq.collect (fun e -> e.Subscriptions)
                    |> Seq.map _.ToDomain()
                    |> Result.choose)
        }

    let getIdentifiers (client: Client) =
        async {
            let request = {
                Sql = "SELECT id FROM chats"
                Params = None
            }

            return!
                client
                |> Query.get<int64> request
                |> ResultAsync.map (Seq.map ChatId >> Seq.toList)
        }

    let tryFindById (id: ChatId) (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT id, subscriptions, culture
                    FROM chats
                    WHERE id = @Id
                    """
                Params = Some {| Id = id.Value |}
            }

            return!
                client
                |> Query.get<Chat.Entity> request
                |> ResultAsync.map Seq.tryHead
                |> ResultAsync.bind (Option.toResult (fun e -> e.ToDomain()))
        }

    let findManyByIds (ids: ChatId seq) (client: Client) =
        async {
            let idArray = ids |> Seq.map _.Value |> Seq.toArray

            let request = {
                Sql =
                    """
                    SELECT id, subscriptions, culture
                    FROM chats
                    WHERE id = ANY(@Ids)
                    """
                Params = Some {| Ids = idArray |}
            }

            return!
                client
                |> Query.get<Chat.Entity> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain()) >> Result.choose)
                |> ResultAsync.map List.ofSeq
        }

    let findManyBySubscription (subscriptionId: RequestId) (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT id, subscriptions, culture
                    FROM chats
                    CROSS JOIN jsonb_array_elements(subscriptions) AS sub
                    WHERE sub->>'id' = @SubscriptionId
                    """
                Params =
                    Some {|
                        SubscriptionId = subscriptionId.ValueStr
                    |}
            }

            return!
                client
                |> Query.get<Chat.Entity> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain()) >> Result.choose)
                |> ResultAsync.map List.ofSeq
        }

    let findManyBySubscriptions (subscriptionIds: RequestId seq) (client: Client) =
        async {
            let idArray = subscriptionIds |> Seq.map _.ValueStr |> Seq.toArray

            let request = {
                Sql =
                    """
                    SELECT DISTINCT id, subscriptions, culture
                    FROM chats
                    CROSS JOIN jsonb_array_elements(subscriptions) AS sub
                    WHERE sub->>'id' = ANY(@SubscriptionIds)
                    """
                Params = Some {| SubscriptionIds = idArray |}
            }

            return!
                client
                |> Query.get<Chat.Entity> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain()) >> Result.choose)
                |> ResultAsync.map List.ofSeq
        }

module Command =

    let create (chat: Chat) (client: Client) =
        async {
            match EA.Telegram.DataAccess.Chat.toEntity chat with
            | Error err -> return Error err
            | Ok entity ->
                let sql = {
                    Sql =
                        """
                        INSERT INTO chats (id, subscriptions, culture)
                        VALUES (@Id, @Subscriptions::jsonb, @Culture)
                        """
                    Params = Some entity
                }

                return! client |> Command.execute sql |> ResultAsync.map (fun _ -> chat)
        }

    let update (chat: Chat) (client: Client) =
        async {
            match EA.Telegram.DataAccess.Chat.toEntity chat with
            | Error err -> return Error err
            | Ok entity ->
                let sql = {
                    Sql =
                        """
                        UPDATE chats SET
                            subscriptions = @Subscriptions::jsonb,
                            culture = @Culture
                        WHERE id = @Id
                        """
                    Params = Some entity
                }

                return! client |> Command.execute sql |> ResultAsync.map (fun _ -> chat)
        }

    let updateSeq (chats: Chat seq) (client: Client) =
        async {
            let mutable results = []
            let mutable error = None

            for chat in chats do
                match error with
                | Some _ -> ()
                | None ->
                    let! result = update chat client
                    match result with
                    | Ok c -> results <- c :: results
                    | Error e -> error <- Some e

            return
                match error with
                | Some e -> Error e
                | None -> Ok(results |> List.rev)
        }

    let createOrUpdate (chat: Chat) (client: Client) =
        async {
            match EA.Telegram.DataAccess.Chat.toEntity chat with
            | Error err -> return Error err
            | Ok entity ->
                let sql = {
                    Sql =
                        """
                        INSERT INTO chats (id, subscriptions, culture)
                        VALUES (@Id, @Subscriptions::jsonb, @Culture)
                        ON CONFLICT (id) DO UPDATE SET
                            subscriptions = EXCLUDED.subscriptions,
                            culture = EXCLUDED.culture
                        """
                    Params = Some entity
                }

                return! client |> Command.execute sql |> ResultAsync.map (fun _ -> chat)
        }

    let delete (id: ChatId) (client: Client) =
        async {
            let sql = {
                Sql = "DELETE FROM chats WHERE id = @Id"
                Params = Some {| Id = id.Value |}
            }

            return! client |> Command.execute sql |> ResultAsync.map ignore
        }

    let deleteMany (ids: ChatId Set) (client: Client) =
        async {
            let idArray = ids |> Set.map _.Value |> Set.toArray

            let sql = {
                Sql = "DELETE FROM chats WHERE id = ANY(@Ids)"
                Params = Some {| Ids = idArray |}
            }

            return! client |> Command.execute sql |> ResultAsync.map ignore
        }

    let createChatSubscription (chatId: ChatId) (subscription: Subscription) (client: Client) =
        async {
            // First, get the current chat to retrieve existing subscriptions
            let! chatResult = Query.tryFindById chatId client

            match chatResult with
            | Error err -> return Error err
            | Ok None -> return $"The '{chatId}' not found." |> NotFound |> Error
            | Ok(Some chat) ->
                // Add new subscription to the set
                let updatedSubscriptions = chat.Subscriptions |> Set.add subscription

                // Update the chat with new subscriptions
                let updatedChat = {
                    chat with
                        Subscriptions = updatedSubscriptions
                }
                let! result = update updatedChat client
                return result |> Result.map ignore
        }

    let deleteChatSubscription (chatId: ChatId) (subscriptionId: RequestId) (client: Client) =
        async {
            // First, get the current chat to retrieve existing subscriptions
            let! chatResult = Query.tryFindById chatId client

            match chatResult with
            | Error err -> return Error err
            | Ok None -> return $"The '{chatId}' not found." |> NotFound |> Error
            | Ok(Some chat) ->
                // Remove subscription from the set
                let updatedSubscriptions =
                    chat.Subscriptions |> Set.filter (fun s -> s.Id <> subscriptionId)

                // Update the chat with new subscriptions
                let updatedChat = {
                    chat with
                        Subscriptions = updatedSubscriptions
                }
                let! result = update updatedChat client
                return result |> Result.map ignore
        }

    let deleteChatSubscriptions (chatId: ChatId) (subscriptionIds: RequestId Set) (client: Client) =
        async {
            // First, get the current chat to retrieve existing subscriptions
            let! chatResult = Query.tryFindById chatId client

            match chatResult with
            | Error err -> return Error err
            | Ok None -> return $"The '{chatId.ValueStr}' not found." |> NotFound |> Error
            | Ok(Some chat) ->
                // Remove multiple subscriptions from the set
                let updatedSubscriptions =
                    chat.Subscriptions
                    |> Set.filter (fun s -> not (subscriptionIds |> Set.contains s.Id))

                // Update the chat with new subscriptions
                let updatedChat = {
                    chat with
                        Subscriptions = updatedSubscriptions
                }
                let! result = update updatedChat client
                return result |> Result.map ignore
        }

    let deleteSubscriptions (subscriptionIds: RequestId Set) (client: Client) =
        async {
            // Find all chats that have any of the given subscriptions
            let! chatsResult = Query.findManyBySubscriptions subscriptionIds client

            match chatsResult with
            | Error err -> return Error err
            | Ok chats ->
                // For each chat, remove the subscriptions
                let updatedChats =
                    chats
                    |> List.map (fun chat ->
                        let updatedSubscriptions =
                            chat.Subscriptions
                            |> Set.filter (fun s -> not (subscriptionIds |> Set.contains s.Id))
                        {
                            chat with
                                Subscriptions = updatedSubscriptions
                        })

                // Update all chats
                let! result = updateSeq updatedChats client
                return result |> Result.map ignore
        }

    let setCulture (chatId: ChatId) (culture: Culture) (client: Client) =
        async {
            // Try to find the existing chat
            let! chatResult = Query.tryFindById chatId client

            match chatResult with
            | Ok None ->
                // Create new chat if it doesn't exist
                let newChat = {
                    Id = chatId
                    Subscriptions = Set.empty
                    Culture = culture
                }
                let! result = create newChat client
                return result |> Result.map ignore
            | Ok(Some chat) ->
                // Update existing chat's culture
                let sql = {
                    Sql = "UPDATE chats SET culture = @Culture WHERE id = @Id"
                    Params =
                        Some {|
                            Id = chatId.Value
                            Culture = culture.Code
                        |}
                }

                return! client |> Command.execute sql
            | Error err -> return Error err
        }
