# F# Code Style (Workspace-Derived)

This style profile is derived from the current `src/` and `submodules/` codebase.

## Formatting Baseline

- Use Fantomas + `.editorconfig` settings.
- 4-space indentation.
- 120-char maximum line length.
- CRLF line endings.

## Module And Namespace Conventions

- Prefer top-level modules with fully-qualified names:
  - `module EA.Russian.Kdmid.Service`
  - `module EA.Core.DataAccess.Storage.Request`
- Use `namespace` only where needed (for example router organization in Telegram).
- Frequently used attributes:
  - `[<RequireQualifiedAccess>]` for stricter callsites.
  - `[<AutoOpen>]` for core domain/prelude convenience modules.

## Naming Conventions

- Types, modules, DU cases: `PascalCase`.
- Functions/values/locals: `camelCase`.
- Private helpers: `let private ...` near top of module.
- Constants: nested `Constants` modules + `[<Literal>]` names.

## Type And Function Patterns

- Domain models are records and DUs with helper members (`parse`, `create`, `print`).
- State transitions use record copy updates:
  - `{ request with ProcessState = InProcess; Modified = DateTime.UtcNow }`
- Dependencies are strongly typed records of functions, often with `static member create`.
- Favor pipelines and small helpers over deep imperative blocks.

## Error And Async Style

- Use `Result<'T, Error'>` and `Async<Result<'T, Error'>>` as primary flow types.
- Use `ResultBuilder()` / `ResultAsyncBuilder()` plus `ResultAsync` combinators (`bind`, `map`, `apply`).
- Encode domain errors via typed constructors (`NotSupported`, `NotFound`, `Operation`, `Canceled`, etc.).

## Data Access Conventions

- Separate domain and persistence entity mapping in `DataAccess` modules.
- Common shape:
  - `type Entity() = ...`
  - `member ToDomain()`
  - extension members `ToEntity()`
  - `Payload.toDomain` / `Payload.toEntity`
- Keep storage abstraction behind provider-based dispatch modules (`Storage`, `Query`, `Command`).

## Practical Do/Don't

- Do keep F# file order dependency-safe (domain first, composition root last).
- Do keep module boundaries explicit (`Domain`, `DataAccess`, `Client`, `Service`).
- Don't bypass result/async error flow with exceptions except at app entry boundaries.
