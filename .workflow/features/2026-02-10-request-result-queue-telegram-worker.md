# Request Result Queue Table + Telegram Notifier Worker

Brief summary: Add a PostgreSQL-backed queue table for request processing results, push rows from embassy worker `handleProcessResult`, and run a Telegram-side polling worker (1s) to deliver notifications to subscribed chats.

## Context
- Area/module: `src/embassy-access-core/`, `src/embassy-access-worker/`, `src/embassy-access-telegram/`.
- Primary user flow: worker processes embassy requests, produces result events; Telegram app consumes events and notifies chats that subscribed to the service.
- Related requests:
  - Add types similar to `src/embassy-access-core/domain/Request.fs` and `src/embassy-access-core/data_access/request/Request.fs`.
  - Push queue rows from:
    - `src/embassy-access-worker/features/embassies/italian/prenotami/Service.fs` (`handleProcessResult`)
    - `src/embassy-access-worker/features/embassies/russian/kdmid/Service.fs` (`handleProcessResult`)
  - Consume queue rows in Telegram app via a worker lib wired like `src/embassy-access-worker/Program.fs`.
  - Use only `Persistence.Storage.Connection.Configuration` storage for DB access in the Telegram-side worker.
  - Use Telegram domain binding via `src/embassy-access-telegram/domain/Subscription.fs` and `src/embassy-access-telegram/domain/Chat.fs`.
  - Message content can follow existing `handleProcessResult` patterns, but does not necessarily have to:
    - `src/embassy-access-telegram/features/embassies/italian/prenotami/Command.fs`
    - `src/embassy-access-telegram/features/embassies/russian/kdmid/Command.fs`
    - `src/embassy-access-telegram/features/embassies/russian/midpass/Command.fs`

## Goals
- Persist worker processing outcomes as durable queue events in PostgreSQL.
- Deliver Telegram notifications from the Telegram app by polling the queue (every 1 second).
- Route notifications to the correct chats using existing `Chat` and `Subscription` types.
- Keep embassy worker processing independent of Telegram delivery (no direct Telegram calls in worker modules).
- One telegram handler can fan out to multiple chats based on subscriptions based on `ServiceId`.

## Non-Goals
- Replacing the existing request storage model (`EA.Core.DataAccess.Request`) or changing `Request<'payload>` shape.

## Requirements
- Add a new PostgreSQL table to act as a queue of "request result events".
  - Must contain enough information to determine:
    - Which service it belongs to `ServiceId`
    - Type of event (e.g. `NoAppointments`, `HasAppointments`, `HasConfirmation`, `Failed`, etc.)
    - Message content that should be sent to users. We should build it in `src/embassy-access-worker/`
  - Must support "at-least-once" delivery semantics (poller can retry safely).
  - Must support marking events through an explicit processing pipeline (not delete-on-send only):
    - example states: `Ready` -> `Processing` -> `Sent` (and `Failed` with retry metadata).
    - include enough metadata for retries and backoff (e.g. attempts count, last error, updated timestamps).
- Core needs domain + data-access abstractions for the new table (mirroring the `Request` patterns).
- Embassy worker modules must enqueue events in `handleProcessResult` for:
  - Italian Prenotami (`NoAppointments`, `HasAppointments`)
  - Russian Kdmid (`NoAppointments`, `HasAppointments`, `HasConfirmation`, `Failed`)
- Telegram app must add a background worker task:
  - Uses the existing configuration pattern (like `src/embassy-access-worker/Program.fs`).
  - Uses only `Persistence.Storage.Connection.Configuration` for DB access.
  - Polls every 1 second.
  - For each queued event, finds matching chats using `Chat.Subscriptions` by `ServiceId`.
  - Sends messages through the existing Telegram producer/client abstractions.
  - Handle events by batch

## Acceptance Criteria
- When Italian Prenotami worker processes a request and determines `NoAppointments` or `HasAppointments`, a queue row is inserted.
- When Russian Kdmid worker processes a request and determines `NoAppointments`, `HasAppointments`, `HasConfirmation`, or `Failed`, a queue row is inserted.
- Telegram app worker polls the queue every 1 second and:
  - Delivers a message to every chat subscribed to the related service.
  - Advances the row to `Sent` (or equivalent terminal success state) after successful send.
- Queue consumption is resilient:
  - A failed send does not permanently lose the event.
  - Re-processing an event does not spam users (idempotency strategy defined and implemented).

## Tasks
0. [x] Prepare specific branches for core, worker, and telegram changes in the `embassy-access` repo:
  - `features/request-result-queue`
1. [ ] Define the queue table schema + migration approach
  Done:
2. [ ] Add core domain model for a "request result event" queue item
  Done:
3. [ ] Add core data-access module for the queue table
  Done:
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
7. [ ] Implement event processing logic in Telegram worker
  Done:
  - (pending) For batched events, find matching chats using `Chat.Subscriptions` by `ServiceId`.
  - (pending) Send messages through the existing Telegram producer/client abstractions.
  - (pending) Advance the row to `Sent` (or equivalent terminal success state) after successful send.
