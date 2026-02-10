# Repository Guidelines

## Project Structure & Module Organization

- `src/`
  - `src/embassy-access-core/`: shared domain models + data-access abstractions.
  - `src/embassy-access-modules/`: embassy-specific modules (e.g. `embassy-access-russian/`, `embassy-access-italian/`).
  - `src/embassy-access-worker/`: primary background worker entry point.
  - `src/embassy-access-telegram/`: optional Telegram bot entry point.
- `submodules/`: foundational F# libraries (infra, persistence, worker engine, web clients, AI provider). Clone/update with:
  - `git submodule update --init --recursive`

## Build, Test, and Development Commands

- `dotnet build`: build the full solution.
- `dotnet test`: run all tests (notably Expecto tests in submodules).
- Worker (local):
  - `cd src/embassy-access-worker; dotnet run`
- Telegram bot (local):
  - `cd src/embassy-access-telegram; dotnet run`
- Format (Fantomas tool manifest in `.config/dotnet-tools.json`):
  - `dotnet tool restore`
  - `dotnet fantomas .`
- Docker:
  - Ensure the external network exists: `docker network create embassy-access-network`
  - `docker compose -f .docker/docker-compose.yml up --build`

## Coding Style & Naming Conventions

- F# files use 4-space indentation and a 120-char line limit (see `.editorconfig`).
- Use Fantomas formatting; keep diffs format-clean (avoid manual alignment wars).
- Prefer clear module/file naming consistent with the repo (e.g. `Router.fs`, `Client.fs`, `Domain.fs`, `Service.fs`).

## Testing Guidelines

- Framework: Expecto (e.g. `submodules/fsharp-worker/tests`).
- Naming: `*Tests.fs` (e.g. `SchedulerTests.fs`).
- Targeted run:
  - `dotnet test submodules/fsharp-worker/tests/fsharp-worker.tests.fsproj`

## Commit & Pull Request Guidelines

- Commit subjects follow a lightweight Conventional Commits style seen in history: `feat: ...`, `fix: ...` (use `docs:`/`chore:` when appropriate).
- PRs should include:
  - What changed + why (1-3 bullets).
  - How to run/verify (exact commands).
  - Any config/env var changes (and defaults/notes).

## Security & Configuration Tips

- Do not commit secrets. Use environment variables or local `.env` files.
- Common required vars (see `.docker/docker-compose.yml`): `POSTGRES_CONNECTION`, `DATA_ENCRYPTION_KEY`, `BROWSER_WEBAPI_URL`, `ANTICAPTCHA_KEY`, `OPENAI_KEY`, `TELEGRAM_BOT_TOKEN`.
- Runtime configuration lives alongside entry points (e.g. `appsettings.yml`, plus `embassies.yml`/`services.yml` for the Telegram app).
