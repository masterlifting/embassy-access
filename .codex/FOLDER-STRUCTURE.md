# Folder Structure (Workspace-Derived)

This structure description is derived from current project files and `.fsproj` compile order.

## Top-Level

- `src/`
  - `embassy-access-core/`
  - `embassy-access-modules/`
  - `embassy-access-worker/`
  - `embassy-access-telegram/`
- `submodules/`
  - `fsharp-infrastructure/`
  - `fsharp-persistence/`
  - `fsharp-worker/`
  - `fsharp-web/`
  - `fsharp-ai-provider/`

## `src/` Project Roles

- `src/embassy-access-core`
  - `domain/`: shared domain primitives and business types.
  - `data_access/`: storage mappers and adapters.
- `src/embassy-access-modules`
  - `embassy-access-russian/` and `embassy-access-italian/`
  - each module follows `Router -> Domain -> DataAccess -> Client -> Service`.
- `src/embassy-access-worker`
  - `shared/`: environment + worker dependency composition.
  - `features/`: embassy-specific worker integrations.
  - `app/`: initializer + handler tree.
  - `Program.fs`: entry point.
- `src/embassy-access-telegram`
  - `shared/`: config/router.
  - `domain/`, `data_access/`, `dependencies/`.
  - `features/`: culture + embassies.
  - `app/`: client/controller/initializer.
  - `Program.fs`: entry point.

## Feature-Slice Placement Rules

- Each feature folder is the primary boundary for feature behavior.
- Keep feature-related code inside its feature slice (query/command/dependencies/controller/helpers).
- Add new feature behavior to the relevant `features/...` path first; avoid scattering across unrelated folders.

If code is shared by multiple features, place it in shared locations instead of a single feature slice:

- Project-level shared runtime/config/composition: `shared/`, `dependencies/`, `app/`.
- Cross-project/shared domain and data contracts: `src/embassy-access-core/`.
- Broad reusable infrastructure/platform code: `submodules/`.

## Submodule Layering

- `fsharp-infrastructure`: domain primitives, prelude, config, logging, serialization.
- `fsharp-persistence`: provider abstractions + storage backends.
- `fsharp-worker`: task scheduling/execution engine.
- `fsharp-web`: HTTP, Telegram, Browser-WebAPI clients.
- `fsharp-ai-provider`: AI integrations and culture features.

## Compile Order Convention (Important In F#)

Across projects, `.fsproj` files follow explicit dependency order:

1. `domain/*`
2. `data_access/*`
3. `dependencies/*` (when present)
4. feature/app orchestration files
5. `Program.fs` last (for executable projects)

## File Role Conventions

Common filenames seen repeatedly:

- `Domain.fs`
- `DataAccess.fs`
- `Client.fs`
- `Service.fs`
- `Router.fs`
- `Dependencies.fs`
- `Initializer.fs`
- `Controller.fs`
- `Query.fs`
- `Command.fs`
- `Storage.fs`

## Non-Source Directories

- Ignore `bin/` and `obj/` when analyzing architecture or style.
