<!-- @format -->

# Embassy Access (F#) | dev

## Overview

Embassy Access is a project designed to facilitate the process of finding and booking appointments at various embassies and consulates. This includes appointments for visas, passport issuance, insurance signing, and other consulate services.

## Project Structure

The project is structured into several components, including core functionality, web APIs, worker services, and submodules for infrastructure, persistence, and web functionalities.

### Solution Structure

- **Submodules**

  - `fsharp-infrastructure`: Provides foundational infrastructure services.
  - `fsharp-persistence`: Handles data persistence.
  - `fsharp-worker`: Manages background tasks and processing.
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
  - `fsharp-web`: Web-related functionalities.
  - `fharp-ai-provider`: Provides AI clients and services.

- **Sources**

  - **embassy-access-modules**
    - `embassy-access-russian`: Logic for the Russian embassy.
    - `embassy-access-russian-tests`: Tests for the Russian embassy module.
  - `embassy-access-core`: Common code for any embassy.
  - `embassy-access-telegram`: Telegram bot for interacting with users.
  - `embassy-access-worker`: Background services for processing tasks for any embassy.

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

Now the `embassy-access-worker` is a single entry point for the application. 
It can be run using the following command:

You should configure the settings in the `appsettings.yaml` file before running the application.
Use the `appsettings.yaml` file as a template.

```bash
cd src/embassy-access-worker
dotnet run
dotenv -e .env -- dotnet run
```

### Contributing

Contributions are welcome! Please fork the repository and submit a pull request.

### License

This project is licensed under the MIT License.

### Contact

For any inquiries or issues, please contact me via telegram at @andreipestunov

