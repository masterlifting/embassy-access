module EA.Core.DataAccess.Postgre.Request

open Infrastructure.Prelude
open EA.Core.Domain
open EA.Core.DataAccess
open Persistence.Storages.Postgre
open Persistence.Storages.Domain.Postgre

module Query =

    let tryFindById (id: RequestId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT
                        id as "Id",
                        service_id as "ServiceId",
                        service_name as "ServiceName",
                        service_description as "ServiceDescription",
                        embassy_id as "EmbassyId",
                        embassy_name as "EmbassyName",
                        embassy_description as "EmbassyDescription",
                        embassy_timezone as "EmbassyTimeZone",
                        payload::text as "Payload",
                        process_state::text as "ProcessState",
                        limits::text as "Limits",
                        created as "Created",
                        modified as "Modified"
                    FROM requests
                    WHERE id = @Id
                """
                Params = Some {| Id = id.Value |}
            }

            return!
                client
                |> Query.get<Request.Entity> request
                |> ResultAsync.map Seq.tryHead
                |> ResultAsync.bind (Option.toResult (fun e -> e.ToDomain payloadConverter))
        }

    let findManyByEmbassyId (embassyId: EmbassyId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT
                        id as "Id",
                        service_id as "ServiceId",
                        service_name as "ServiceName",
                        service_description as "ServiceDescription",
                        embassy_id as "EmbassyId",
                        embassy_name as "EmbassyName",
                        embassy_description as "EmbassyDescription",
                        embassy_timezone as "EmbassyTimeZone",
                        payload::text as "Payload",
                        process_state::text as "ProcessState",
                        limits::text as "Limits",
                        created as "Created",
                        modified as "Modified"
                    FROM requests
                    WHERE embassy_id = @EmbassyId
                """
                Params = Some {| EmbassyId = embassyId.Value |}
            }

            return!
                client
                |> Query.get<Request.Entity> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

    let findManyByServiceId (serviceId: ServiceId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT
                        id as "Id",
                        service_id as "ServiceId",
                        service_name as "ServiceName",
                        service_description as "ServiceDescription",
                        embassy_id as "EmbassyId",
                        embassy_name as "EmbassyName",
                        embassy_description as "EmbassyDescription",
                        embassy_timezone as "EmbassyTimeZone",
                        payload::text as "Payload",
                        process_state::text as "ProcessState",
                        limits::text as "Limits",
                        created as "Created",
                        modified as "Modified"
                    FROM requests
                    WHERE service_id = @ServiceId
                """
                Params = Some {| ServiceId = serviceId.Value |}
            }

            return!
                client
                |> Query.get<Request.Entity> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

    let findManyStartWithServiceId (serviceId: ServiceId) payloadConverter (client: Client) =
        async {
            let request = {
                Sql =
                    """
                    SELECT
                        id as "Id",
                        service_id as "ServiceId",
                        service_name as "ServiceName",
                        service_description as "ServiceDescription",
                        embassy_id as "EmbassyId",
                        embassy_name as "EmbassyName",
                        embassy_description as "EmbassyDescription",
                        embassy_timezone as "EmbassyTimeZone",
                        payload::text as "Payload",
                        process_state::text as "ProcessState",
                        limits::text as "Limits",
                        created as "Created",
                        modified as "Modified"
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
                |> Query.get<Request.Entity> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

    let findManyByIds (ids: RequestId seq) payloadConverter (client: Client) =
        async {
            let idArray = ids |> Seq.map _.Value |> Seq.toArray

            let request = {
                Sql =
                    """
                    SELECT
                        id as "Id",
                        service_id as "ServiceId",
                        service_name as "ServiceName",
                        service_description as "ServiceDescription",
                        embassy_id as "EmbassyId",
                        embassy_name as "EmbassyName",
                        embassy_description as "EmbassyDescription",
                        embassy_timezone as "EmbassyTimeZone",
                        payload::text as "Payload",
                        process_state::text as "ProcessState",
                        limits::text as "Limits",
                        created as "Created",
                        modified as "Modified"
                    FROM requests
                    WHERE id = ANY(@Ids)
                """
                Params = Some {| Ids = idArray |}
            }

            return!
                client
                |> Query.get<Request.Entity> request
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
                    SELECT 
                        id as "Id",
                        service_id as "ServiceId", 
                        service_name as "ServiceName",
                        service_description as "ServiceDescription",
                        embassy_id as "EmbassyId",
                        embassy_name as "EmbassyName",
                        embassy_description as "EmbassyDescription",
                        embassy_timezone as "EmbassyTimeZone",
                        payload::text as "Payload",
                        process_state::text as "ProcessState",
                        limits::text as "Limits",
                        created as "Created",
                        modified as "Modified"
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
                |> Query.get<Request.Entity> request
                |> ResultAsync.bind (Seq.map (fun e -> e.ToDomain payloadConverter) >> Result.choose)
        }

module Command =
    let upsert (request: Request<_>) payloadConverter (client: Client) =
        async {
            match request |> Request.toEntity payloadConverter with
            | Error err -> return Error err
            | Ok entity ->
                let sqlRequest = {
                    Sql =
                        """
                        INSERT INTO requests (
                            id, 
                            service_id, 
                            service_name, 
                            service_description,
                            embassy_id, 
                            embassy_name, 
                            embassy_description, 
                            embassy_timezone,
                            payload, 
                            process_state, 
                            limits, 
                            created, 
                            modified
                        ) VALUES (
                            @Id, 
                            @ServiceId, 
                            @ServiceName::text[], 
                            @ServiceDescription,
                            @EmbassyId, 
                            @EmbassyName::text[], 
                            @EmbassyDescription, 
                            @EmbassyTimeZone,
                            @Payload::jsonb, 
                            @ProcessState::jsonb, 
                            @Limits::jsonb, 
                            @Created, 
                            @Modified
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

                return! client |> Command.execute sqlRequest |> ResultAsync.map (fun _ -> request)
        }

    let delete (id: RequestId) (client: Client) =
        async {
            let request = {
                Sql = "DELETE FROM requests WHERE id = @Id"
                Params = Some {| Id = id.Value |}
            }

            return! client |> Command.execute request |> ResultAsync.map ignore
        }

module Migrations =

    let private initial (client: Client) =
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

            return! client |> Command.execute migration |> ResultAsync.map ignore
        }

    let private clean (client: Client) =
        async {
            client |> Provider.dispose
            return Ok()
        }

    let apply (connectionString: string) =
        {
            String = connectionString
            Lifetime = Persistence.Domain.Transient
        }
        |> Provider.init
        |> ResultAsync.wrap (fun client -> client |> initial |> ResultAsync.apply (client |> clean))
