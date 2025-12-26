<!-- @format -->

# Embassy Access (F#) | dev

## Overview

Embassy Access is a project designed to facilitate the process of finding and booking appointments at various embassies and consulates. This includes appointments for visas, passport issuance, insurance signing, and other consulate services.

## Project Structure

The project is organized into a main application with multiple submodules providing foundational libraries.

### Architecture Overview

```
embassy-access (main repo)
├── src/
│   ├── embassy-access-core         # Domain models & data access abstractions
│   ├── embassy-access-modules/
│   │   ├── embassy-access-russian  # Russian embassy services (KDMID, MIDPass)
│   │   └── embassy-access-italian  # Italian embassy services (Prenotami)
│   ├── embassy-access-worker       # Main worker service (entry point)
│   └── embassy-access-telegram     # Telegram bot interface (optional)
└── submodules/
    ├── fsharp-infrastructure       # Configuration, logging, utilities
    ├── fsharp-persistence          # Storage abstractions (PostgreSQL, FileSystem, etc.)
    ├── fsharp-worker              # Task scheduling & execution engine
    ├── fsharp-web                 # HTTP, Telegram, Browser clients
    └── fsharp-ai-provider         # AI services (OpenAI, translations)
```

### Solution Structure

- **Submodules**

  - `fsharp-infrastructure`: Foundational infrastructure services including configuration management (YAML/JSON), logging (console/file), utility functions (async, result, tree, string helpers), and serialization.
  - `fsharp-persistence`: Data persistence layer with storage abstractions supporting PostgreSQL, FileSystem, InMemory, and Configuration-based storage.
  - `fsharp-worker`: Task scheduling and execution engine with hierarchical workflows, per-node scheduling, recursion, and parallel/sequential execution support.
<!-- WORKER_DOC_START -->
# F# Worker (Updated)

Executes a directed task tree with mixed parallel/sequential groups, per-node scheduling and optional recursion (time-based re-run). Stateless: only invokes user handlers; task state lives in configured storage.

TaskNode fields: Enabled, Recursively(TimeSpan), Parallel(bool), Duration(TimeSpan, default 2m), WaitResult(bool), Schedule(optional), Description(optional). Handlers are attached by Id; merging builds WorkerTask with same fields plus Handler option.

Schedule: Name, Start/Stop Date/Time, Workdays set, Recursively(bool for schedule window continuation), TimeZone(hour offset). Scheduler resolves to Started | StartIn(delay) | StopIn(delay) | Stopped | NotScheduled and supports recursive windows.

Execution flow: initialize storage (Postgre applies migrations & optional task insert; Configuration just loads). Start from RootTaskId, process: take leading Parallel=true block (>=2 -> Async.Parallel else sequential group), then recurse remaining list. Each task start: schedule evaluation, optional delay, handler run with cancellation by Duration, optional fire-and-forget if WaitResult=false. Recursively(TimeSpan) reschedules same task after delay.

Storage supported: Postgre database, Configuration (in-memory from config). FindTask pulls persisted/memory tree, merges with handlers tree, then walks children.

Program.fs constructs Worker.Client.start with: Name, RootTaskId ("WRK"), Storage, Tasks(optional seed), Handlers, TaskDeps (custom shared deps). Returns async run.

See submodules/fsharp-worker/src for full implementation details.
<!-- WORKER_DOC_END -->
  - `fsharp-web`: Web-related functionalities (HTTP client, Telegram bot client, Browser WebAPI, AntiCaptcha service).
  - `fsharp-ai-provider`: Provides AI clients and services (OpenAI integration, culture/translation features).

- **Sources**

  - **embassy-access-modules**
    - `embassy-access-russian`: Logic for Russian embassy appointment services (KDMID and MIDPass).
    - `embassy-access-italian`: Logic for Italian embassy appointment services (Prenotami).
  - `embassy-access-core`: Common domain models and data access abstractions for all embassies.
  - `embassy-access-telegram`: Telegram bot application for interacting with users (optional entry point).
  - `embassy-access-worker`: Main background worker service for processing embassy appointment tasks.

## Getting Started

### Prerequisites

- .NET 10 SDK/Runtime
- F# experience

### Cloning the Repository

```bash
git clone https://github.com/masterlifting/embassy-access --recurse-submodules
```

### Building the Solution

```bash
cd embassy-access
dotnet build
```

### Running the Application

The application provides two entry points:

#### 1. Worker Service (Main Entry Point)

The `embassy-access-worker` is the primary entry point for automated embassy appointment processing. 
It runs as a background service that executes tasks based on configured schedules.

Configure the settings in the `appsettings.yml` file before running:

```bash
cd src/embassy-access-worker
dotnet run
# Or with environment variables:
dotenv -e .env -- dotnet run
```

#### 2. Telegram Bot (Optional)

The `embassy-access-telegram` provides an interactive Telegram bot interface for users.
This is an optional component that can be run separately.

Configure the settings in the `appsettings.yml`, `embassies.yml`, and `services.yml` files:

```bash
cd src/embassy-access-telegram
dotnet run
# Or with environment variables:
dotenv -e .env -- dotnet run
```

### Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

### License

This project is licensed under the MIT License.

### Contact

For any inquiries or issues, please contact me via telegram at @andreipestunov

