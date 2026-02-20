# Repository Guidelines

## Workspace Configuration Provider

- Primary workspace configuration source: `.codex/README.md`.
- Treat `.codex/README.md` and its referenced documents as authoritative for:
  - style conventions
  - folder structure
  - architecture patterns
  - tool/skill references
- Enforce feature-slice architecture and shared-infrastructure placement rules from `.codex/ARCHITECTURE.md` and `.codex/FOLDER-STRUCTURE.md`.

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
  - `cd src/embassy-access-worker; dotenv -e .env -- dotnet run`
- Telegram bot (local):
  - `cd src/embassy-access-telegram; dotenv -e .env -- dotnet run`
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

## Task Tracking

- For task tracking workflow, naming, structure, and completion rules, follow the global task skill:
  - `$HOME/.codex/skills/task/SKILL.md`

## Codex Skills & Instructions

- Common reusable skills belong to global `$HOME/.codex/skills`.
- Repository-specific rules/skills belong to local `.codex/` in this repository.
- This repository currently uses the global `task` skill (no local task skill).
- Use `.codex/README.md` as the index for workspace-local configuration references.

## Instruction References Policy

- For new instructions, prefer references to global/local skills or tools instead of duplicating full procedures in `AGENTS.md`.
- Keep `AGENTS.md` concise and use path references as the source of truth when possible.

## Security & Configuration Tips

- Do not commit secrets. Use environment variables or local `.env` files but don't read them.
- Common required vars (see `.docker/docker-compose.yml`): `POSTGRES_CONNECTION`, `DATA_ENCRYPTION_KEY`, `BROWSER_WEBAPI_URL`, `ANTICAPTCHA_KEY`, `OPENAI_KEY`, `TELEGRAM_BOT_TOKEN`.
- Runtime configuration lives alongside entry points (e.g. `appsettings.yml`, plus `embassies.yml`/`services.yml` for the Telegram app).
