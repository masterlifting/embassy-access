# Request Result Queue Table + Telegram Notifier Worker

Brief summary: Add a PostgreSQL-backed queue table for request processing results, push rows from embassy worker `handleProcessResult`, and run a Telegram-side polling worker (1s) to deliver notifications to subscribed chats.

## Context
- Area/module: `src/embassy-access-core/`, `src/embassy-access-worker/`, `src/embassy-access-telegram/`.
- Primary user flow: worker processes embassy requests, produces result events; Telegram app consumes events and notifies chats that subscribed to the request/service/embassy.
- Related requests:
  - Add types similar to `src/embassy-access-core/domain/Request.fs` and `src/embassy-access-core/data_access/request/Request.fs`.
  - Push queue rows from:
    - `src/embassy-access-worker/features/embassies/italian/prenotami/Service.fs` (`handleProcessResult`)
    - `src/embassy-access-worker/features/embassies/russian/kdmid/Service.fs` (`handleProcessResult`)
  - Consume queue rows in Telegram app via a worker lib wired like `src/embassy-access-worker/Program.fs`.
  - Use only `Persistence.Storage.Connection.Configuration` storage for DB access in the Telegram-side worker.
  - Use Telegram domain binding via `src/embassy-access-telegram/domain/Subscription.fs` and `src/embassy-access-telegram/domain/Chat.fs`.
  - Message content should follow existing `handleProcessResult` patterns:
    - `src/embassy-access-telegram/features/embassies/italian/prenotami/Command.fs`
    - `src/embassy-access-telegram/features/embassies/russian/kdmid/Command.fs`
    - `src/embassy-access-telegram/features/embassies/russian/midpass/Command.fs`

## Goals
- Persist worker processing outcomes as durable queue events in PostgreSQL.
- Deliver Telegram notifications from the Telegram app by polling the queue (every 1 second).
- Route notifications to the correct chats using existing `Chat` and `Subscription` types.
- Keep embassy worker processing independent of Telegram delivery (no direct Telegram calls in worker modules).

## Non-Goals
- Replacing the existing request storage model (`EA.Core.DataAccess.Request`) or changing `Request<'payload>` shape.
- Implementing new Telegram UX flows beyond sending the same kinds of messages already produced in `handleProcessResult`.
- Implementing Midpass spreading if it is currently not implemented (`src/embassy-access-telegram/features/embassies/russian/midpass/Command.fs`).

## Requirements
- Add a new PostgreSQL table to act as a queue of "request result events".
  - Must contain enough information to determine:
    - Which request the event is for (`RequestId`)
    - Which embassy/service it belongs to (`EmbassyId`, `ServiceId`)
    - What kind of event/result it represents (e.g. "no appointments", "appointments found", "confirmation found", "failed")
    - Payload needed to render Telegram messages as a fully serialized event type (Telegram should not need to re-load requests to render messages).
  - Must support "at-least-once" delivery semantics (poller can retry safely).
  - Must support marking events through an explicit processing pipeline (not delete-on-send only):
    - example states: `Created` -> `Processing` -> `Sent` (and `Failed` with retry metadata).
    - include enough metadata for retries and backoff (e.g. attempts count, last error, updated timestamps).
- Core needs domain + data-access abstractions for the new table (mirroring the `Request` patterns).
- Embassy worker modules must enqueue events in `handleProcessResult` for:
  - Italian Prenotami (`NoAppointments`, `HasAppointments`)
  - Russian Kdmid (`NoAppointments`, `HasAppointments`, `HasConfirmation`, `Failed`)
- Responsibility split:
  - `embassy-access-worker` decides what events to push (event kind + minimal event payload, serialized).
  - `embassy-access-telegram` decides how to send (rendering and message layout in each `Command.fs`).
- Telegram app must add a background worker task:
  - Uses the existing configuration pattern (like `src/embassy-access-worker/Program.fs`).
  - Uses only `Persistence.Storage.Connection.Configuration` for DB access.
  - Polls every 1 second.
  - For each queued event, finds matching chats using `Chat.Subscriptions` by exact `Subscription.Id = RequestId` (not by `(ServiceId, EmbassyId)` fan-out).
  - Sends messages through the existing Telegram producer/client abstractions.

## Acceptance Criteria
- When Italian Prenotami worker processes a request and determines `NoAppointments` or `HasAppointments`, a queue row is inserted.
- When Russian Kdmid worker processes a request and determines `NoAppointments`, `HasAppointments`, `HasConfirmation`, or `Failed`, a queue row is inserted.
- Telegram app worker polls the queue every 1 second and:
  - Delivers a message to every chat subscribed to the related request/service/embassy.
  - Advances the row to `Sent` (or equivalent terminal success state) after successful send.
- Queue consumption is resilient:
  - A failed send does not permanently lose the event.
  - Re-processing an event does not spam users (idempotency strategy defined and implemented).

## Tasks
1. [ ] Define the queue table schema + migration approach
  Done:
  - 2026-02-10: Payload strategy decided: store a fully serialized event type in the queue row (no request reload needed for rendering).
  - 2026-02-10: Recipient binding decided: Telegram sends only to chats with `Subscription.Id = RequestId`.
  - 2026-02-10: Processing strategy decided: explicit pipeline states (e.g. `Created`/`Processing`/`Sent`/`Failed`) with retry metadata.
  - 2026-02-10: Responsibility split decided: worker enqueues events; Telegram determines message formatting in `Command.fs`.
2. [ ] Add core domain model for a "request result event" queue item
  Done:
  - (pending) Add types under `src/embassy-access-core/domain/` (mirroring `RequestId` and `Request<'payload>` conventions).
3. [ ] Add core data-access module for the queue table
  Done:
  - (pending) Add types and converters under `src/embassy-access-core/data_access/` similar to `src/embassy-access-core/data_access/request/Request.fs`.
4. [ ] Enqueue events from Italian Prenotami worker
  Done:
  - (pending) Update `src/embassy-access-worker/features/embassies/italian/prenotami/Service.fs` `handleProcessResult` to insert queue rows.
5. [ ] Enqueue events from Russian Kdmid worker
  Done:
  - (pending) Update `src/embassy-access-worker/features/embassies/russian/kdmid/Service.fs` `handleProcessResult` to insert queue rows.
6. [ ] Add Telegram-side polling worker library + wire it into Telegram app
  Done:
  - (pending) Add a background task entry point in `src/embassy-access-telegram/` similar to how worker tasks are wired in `src/embassy-access-worker/Program.fs`.
  - (pending) Use only `Persistence.Storage.Connection.Configuration` for DB connection.
  - (pending) Poll every 1 second.
7. [ ] Implement event-to-message rendering and spreading using existing command patterns
  Done:
  - (pending) Reuse message logic patterns from:
    - `src/embassy-access-telegram/features/embassies/italian/prenotami/Command.fs`
    - `src/embassy-access-telegram/features/embassies/russian/kdmid/Command.fs`
8. [ ] Add tests for queue enqueue/dequeue behavior
  Done:
  - (pending) Cover ordering, retries, idempotency, and chat binding via subscriptions.
