module EA.Telegram.DataAccess.Postgre.Chat

open Infrastructure.Domain
open Infrastructure.Prelude
open Web.Clients.Domain.Telegram
open EA.Telegram.Domain
open EA.Telegram.DataAccess
open Persistence.Storages.Postgre
open Persistence.Storages.Domain.Postgre

module Query =

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

                return! client |> Command.execute sql |> ResultAsync.map ignore
            | Error err -> return Error err
        }

module Migrations =

    let private init (client: Client) =
        async {
            let migration = {
                Sql =
                    """
                    CREATE TABLE IF NOT EXISTS chats (
                        id BIGINT PRIMARY KEY,
                        subscriptions JSONB NOT NULL,
                        culture VARCHAR(10) NOT NULL
                    );
                    """
                Params = None
            }

            return! client |> Command.execute migration
        }

    let apply (client: Client) =
        async {
            match! init client with
            | Ok _ -> return ()
            | Error err -> failwithf "Failed to apply migrations for chats: %A" err
        }