# Architecture Patterns (Workspace-Derived)

This architecture summary is inferred from executable entry points, feature modules, and shared submodules.

## Architectural Layers

1. Application entry points (`Program.fs` in worker/telegram).
2. Feature orchestration (`app/`, `features/`, controllers/handlers).
3. Domain/service modules (`Domain`, `Service`, `Router`, `Client`).
4. Data access adapters (`DataAccess`, `Storage`, `Query`, `Command`).
5. Shared infrastructure (submodules: infra/persistence/worker/web/ai).

## Feature-Slice Rule (Required)

- Use feature-slice architecture by default.
- All feature-specific code must live inside that feature folder (for example under `features/...`):
  - feature dependencies
  - query/command handlers
  - feature controller/orchestration
  - feature-specific mapping/helpers
- Do not spread feature behavior across unrelated top-level folders when it is not shared.

## Composition Root Pattern

Both executables follow this flow:

1. Load configuration files.
2. Initialize logging.
3. Build typed dependency records (`Dependencies.create`).
4. Run initialization/migrations.
5. Start runtime client (`Worker.Client.start` or `EA.Telegram.Client.start`).

## Dependency Injection Style

Dependency injection is function-oriented, not container-oriented:

- `type Dependencies = { ... function fields ... }`
- `static member create ...`
- modules consume dependencies as explicit arguments.

This keeps runtime wiring explicit and testable.

## Result/Async Pipeline Architecture

Main control flow uses:

- `Result<'T, Error'>`
- `Async<Result<'T, Error'>>`
- `ResultBuilder` / `ResultAsyncBuilder` + combinators.

Pattern used broadly:

- validate -> enrich -> process -> persist final state -> cleanup.

## Worker Runtime Pattern

- Tasks are represented as tree nodes with metadata (`Parallel`, `Duration`, `Schedule`, `WaitResult`).
- Handlers are merged with task tree at runtime.
- Scheduler computes `Started | StartIn | StopIn | Stopped | NotScheduled`.
- Execution supports mixed sequential + parallel task groups.

## Router Pattern (Telegram + Embassy Modules)

- Route values are modeled as nested DUs with `parse` + `Value` members.
- Incoming user payloads are parsed into strongly typed routes.
- Controllers dispatch on route DU cases.
- Feature modules handle query/command flows.

## Data Access Pattern

- Domain and persistence entities are separated explicitly.
- Conversion functions sit in `DataAccess` modules (`ToDomain`, `ToEntity`).
- Storage uses provider abstraction (FileSystem/InMemory/Postgre/Configuration).
- Query and command concerns are split into submodules.

## Shared Infrastructure Placement

If code is shared across multiple features, it should be placed outside feature slices:

- App-level shared composition/config: `shared/`, `dependencies/`, or `app/` in entry projects.
- Cross-feature domain/data abstractions: `src/embassy-access-core/`.
- Reusable platform libraries: `submodules/` (`fsharp-infrastructure`, `fsharp-persistence`, `fsharp-worker`, `fsharp-web`, `fsharp-ai-provider`).

## Cross-Repository Module Contracts

- `EA.Core` provides shared domain + storage abstractions.
- Embassy modules (`EA.Russian`, `EA.Italian`) encapsulate integration logic.
- Worker consumes embassy services for automated processing.
- Telegram app consumes embassy/core services for user interaction.
- Submodules provide reusable building blocks without leaking app-specific behavior.
