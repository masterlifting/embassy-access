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
                    SELECT 
                        id as "Id", 
                        subscriptions as "Subscriptions", 
                        culture as "Culture"
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
            match Chat.toEntity chat with
            | Error err -> return Error err
            | Ok entity ->
                let request = {
                    Sql =
                        """
                        INSERT INTO chats (id, subscriptions, culture)
                        VALUES (@Id, @Subscriptions::jsonb, @Culture)
                    """
                    Params = Some entity
                }

                return! client |> Command.execute request |> ResultAsync.map (fun _ -> chat)
        }

    let update (chat: Chat) (client: Client) =
        async {
            match Chat.toEntity chat with
            | Error err -> return Error err
            | Ok entity ->
                let request = {
                    Sql =
                        """
                        UPDATE chats SET
                            subscriptions = @Subscriptions::jsonb,
                            culture = @Culture
                        WHERE id = @Id
                    """
                    Params = Some entity
                }

                return! client |> Command.execute request |> ResultAsync.map (fun _ -> chat)
        }

    let setCulture (chatId: ChatId) (culture: Culture) (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    INSERT INTO chats (id, subscriptions, culture)
                    VALUES (@Id, @Subscriptions::jsonb, @Culture)
                    ON CONFLICT (id) DO UPDATE SET
                        culture = EXCLUDED.culture
                """
                Params =
                    Some {|
                        Id = chatId.Value
                        Subscriptions = "[]" // Default empty subscriptions as JSONB
                        Culture = culture.Code
                    |}
            }

            return! client |> Command.execute request |> ResultAsync.map ignore
        }

module Migrations =

    let private initial (client: Client) =
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

            return! client |> Command.execute migration |> ResultAsync.map ignore
        }

    let private clean (client: Client) =
        async {
            client |> Provider.dispose
            return Ok()
        }

    let apply (connectionString: string) =
        Provider.init {
            String = connectionString
            Lifetime = Persistence.Domain.Transient
        }
        |> ResultAsync.wrap (fun client -> client |> initial |> ResultAsync.applyAsync (client |> clean))
