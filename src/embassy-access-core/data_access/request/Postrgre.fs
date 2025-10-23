module EA.Core.DataAccess.Postgre.Request

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Persistence.Storages.Postgre
open Persistence.Storages.Domain.Postgre

module Query =

    let getIdentifiers (client: Client) =
        async {
            let request = {
                Sql = "SELECT id FROM requests"
                Params = None
            }

            return!
                client
                |> Query.get<string> request
                |> ResultAsync.bind (Seq.map RequestId.create >> Result.choose)
        }

    let tryFindById (id: RequestId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT id, service_id, service_name, service_description,
                           embassy_id, embassy_name, embassy_description, embassy_timezone,
                           payload, process_state, limits, created, modified
                    FROM requests
                    WHERE id = @Id
                """
                Params = Some {| Id = id.ValueStr |}
            }

            return!
                client
                |> Query.get<Request.Entity<_>> request
                |> ResultAsync.map Seq.tryHead
                |> ResultAsync.bind (Option.toResult (fun e -> e.ToDomain payloadConverter))
        }

    let findManyByEmbassyId (embassyId: EmbassyId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT id, service_id, service_name, service_description,
                           embassy_id, embassy_name, embassy_description, embassy_timezone,
                           payload, process_state, limits, created, modified
                    FROM requests
                    WHERE embassy_id = @EmbassyId
                """
                Params = Some {| EmbassyId = embassyId.Value |}
            }

            return!
                client
                |> Query.get<Request.Entity<_>> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

    let findManyByServiceId (serviceId: ServiceId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT id, service_id, service_name, service_description,
                           embassy_id, embassy_name, embassy_description, embassy_timezone,
                           payload, process_state, limits, created, modified
                    FROM requests
                    WHERE service_id = @ServiceId
                """
                Params = Some {| ServiceId = serviceId.Value |}
            }

            return!
                client
                |> Query.get<Request.Entity<_>> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

    let findManyStartWithServiceId (serviceId: ServiceId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT id, service_id, service_name, service_description,
                           embassy_id, embassy_name, embassy_description, embassy_timezone,
                           payload, process_state, limits, created, modified
                    FROM requests
                    WHERE service_id LIKE @ServiceIdPattern
                """
                Params =
                    Some {|
                        ServiceIdPattern = serviceId.Value + "%"
                    |}
            }

            return!
                client
                |> Query.get<Request.Entity<_>> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

    let findManyByIds (ids: RequestId seq) payloadConverter (client: Client) =
        async {
            let idArray = ids |> Seq.map _.ValueStr |> Seq.toArray

            let request = {
                Sql =
                    """
                    SELECT id, service_id, service_name, service_description,
                           embassy_id, embassy_name, embassy_description, embassy_timezone,
                           payload, process_state, limits, created, modified
                    FROM requests
                    WHERE id = ANY(@Ids)
                """
                Params = Some {| Ids = idArray |}
            }

            return!
                client
                |> Query.get<Request.Entity<_>> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
                |> ResultAsync.map List.ofSeq
        }

    let findManyByEmbassyIdAndServiceId
        (embassyId: EmbassyId)
        (serviceId: ServiceId)
        payloadConverter
        (client: Client)
        =
        async {
            let request = {
                Sql =
                    """
                    SELECT id, service_id, service_name, service_description,
                           embassy_id, embassy_name, embassy_description, embassy_timezone,
                           payload, process_state, limits, created, modified
                    FROM requests
                    WHERE embassy_id = @EmbassyId AND service_id = @ServiceId
                """
                Params =
                    Some {|
                        EmbassyId = embassyId.Value
                        ServiceId = serviceId.Value
                    |}
            }

            return!
                client
                |> Query.get<Request.Entity<_>> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

module Command =
    let create (request: Request<_>) payloadConverter (client: Client) =
        async {
            match request |> Request.toEntity payloadConverter with
            | Error err -> return Error err
            | Ok entity ->
                let sql = {
                    Sql =
                        """
                        INSERT INTO requests (
                            id, service_id, service_name, service_description,
                            embassy_id, embassy_name, embassy_description, embassy_timezone,
                            payload, process_state, limits, created, modified
                        ) VALUES (
                            @Id, @ServiceId, @ServiceName::text[], @ServiceDescription,
                            @EmbassyId, @EmbassyName::text[], @EmbassyDescription, @EmbassyTimeZone,
                            @Payload::jsonb, @ProcessState::jsonb, @Limits::jsonb, @Created, @Modified
                        )
                    """
                    Params = Some entity
                }

                return! client |> Command.execute sql |> ResultAsync.map (fun _ -> request)
        }

    let update (request: Request<_>) payloadConverter (client: Client) =
        async {
            match request |> Request.toEntity payloadConverter with
            | Error err -> return Error err
            | Ok entity ->
                let sql = {
                    Sql =
                        """
                        UPDATE requests SET
                            service_id = @ServiceId,
                            service_name = @ServiceName::text[],
                            service_description = @ServiceDescription,
                            embassy_id = @EmbassyId,
                            embassy_name = @EmbassyName::text[],
                            embassy_description = @EmbassyDescription,
                            embassy_timezone = @EmbassyTimeZone,
                            payload = @Payload::jsonb,
                            process_state = @ProcessState::jsonb,
                            limits = @Limits::jsonb,
                            modified = @Modified
                        WHERE id = @Id
                    """
                    Params = Some entity
                }

                return! client |> Command.execute sql |> ResultAsync.map (fun _ -> request)
        }

    let updateSeq (requests: Request<_> seq) payloadConverter (client: Client) =
        async {
            // For batch updates, we'll execute them sequentially
            // Consider using transactions for better atomicity
            let mutable results = []
            let mutable error = None

            for request in requests do
                match error with
                | Some _ -> ()
                | None ->
                    let! result = update request payloadConverter client
                    match result with
                    | Ok r -> results <- r :: results
                    | Error e -> error <- Some e

            return
                match error with
                | Some e -> Error e
                | None -> Ok(results |> List.rev)
        }

    let createOrUpdate (request: Request<_>) payloadConverter (client: Client) =
        async {
            match request |> Request.toEntity payloadConverter with
            | Error err -> return Error err
            | Ok entity ->
                let sql = {
                    Sql =
                        """
                        INSERT INTO requests (
                            id, service_id, service_name, service_description,
                            embassy_id, embassy_name, embassy_description, embassy_timezone,
                            payload, process_state, limits, created, modified
                        ) VALUES (
                            @Id, @ServiceId, @ServiceName::text[], @ServiceDescription,
                            @EmbassyId, @EmbassyName::text[], @EmbassyDescription, @EmbassyTimeZone,
                            @Payload::jsonb, @ProcessState::jsonb, @Limits::jsonb, @Created, @Modified
                        )
                        ON CONFLICT (id) DO UPDATE SET
                            service_id = EXCLUDED.service_id,
                            service_name = EXCLUDED.service_name,
                            service_description = EXCLUDED.service_description,
                            embassy_id = EXCLUDED.embassy_id,
                            embassy_name = EXCLUDED.embassy_name,
                            embassy_description = EXCLUDED.embassy_description,
                            embassy_timezone = EXCLUDED.embassy_timezone,
                            payload = EXCLUDED.payload,
                            process_state = EXCLUDED.process_state,
                            limits = EXCLUDED.limits,
                            modified = EXCLUDED.modified
                    """
                    Params = Some entity
                }

                return! client |> Command.execute sql |> ResultAsync.map (fun _ -> request)
        }

    let delete (id: RequestId) (client: Client) =
        async {
            let sql = {
                Sql = "DELETE FROM requests WHERE id = @Id"
                Params = Some {| Id = id.ValueStr |}
            }

            return! client |> Command.execute sql |> ResultAsync.map ignore
        }

    let deleteMany (ids: RequestId Set) (client: Client) =
        async {
            let idArray = ids |> Set.map _.ValueStr |> Set.toArray

            let sql = {
                Sql = "DELETE FROM requests WHERE id = ANY(@Ids)"
                Params = Some {| Ids = idArray |}
            }

            return! client |> Command.execute sql |> ResultAsync.map ignore
        }

module Migrations =

    let private init (client: Client) =
        async {
            let migration = {
                Sql =
                    """
                    CREATE TABLE IF NOT EXISTS requests (
                        id TEXT PRIMARY KEY,
                        service_id TEXT NOT NULL,
                        service_name TEXT[] NOT NULL,
                        service_description TEXT,
                        embassy_id TEXT NOT NULL,
                        embassy_name TEXT[] NOT NULL,
                        embassy_description TEXT,
                        embassy_timezone TEXT NOT NULL,
                        payload JSONB NOT NULL,
                        process_state JSONB NOT NULL,
                        limits JSONB NOT NULL,
                        created TIMESTAMPTZ NOT NULL,
                        modified TIMESTAMPTZ NOT NULL
                    )
                    """
                Params = None
            }

            return! client |> Command.execute migration
        }

    let apply (client: Client) =
        async {
            match! init client with
            | Ok _ -> return ()
            | Error e -> failwithf "Failed to apply migrations for requests: %A" e
        }
