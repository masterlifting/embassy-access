# Italian Browser-WebApi Error Screenshot Capture

Brief summary: Add error-handling coverage for the Italian Prenotami browser-webapi flow so that when browser-webapi fails, we request a screenshot and persist it under the runtime `debug/` directory as a temporary troubleshooting side effect.

## Context
- Area/module: `src/embassy-access-modules/embassy-access-italian/prenotami/Web.BrowserWebApi.fs`.
- Supporting flow(s): `src/embassy-access-modules/embassy-access-italian/prenotami/Service.fs`, `src/embassy-access-modules/embassy-access-italian/prenotami/Client.fs`.
- Related infra/client: `submodules/fsharp-web/src/clients/browser-webapi/` (if request/contract changes are needed).
- Primary user flow: Worker/Telegram Italian parsing run calls browser-webapi; on remote API failure we keep diagnostic screenshot artifacts.
- Related issues/requests: User request on 2026-02-11 to cover this error path with tests and temporary debug screenshot side effect.

## Goals
- Ensure the Italian browser-webapi error path is explicitly tested.
- Capture a screenshot when browser-webapi returns an error, storing the file under runtime `debug/`.
- Preserve existing failure semantics while adding diagnostics.

## Non-Goals
- Building a permanent observability/retention subsystem.
- Changing unrelated embassy flows.
- Keeping debug artifacts forever without cleanup strategy.

## Requirements
- On browser-webapi failure in the Italian parsing flow, attempt to request a screenshot as best-effort side effect.
- Persist screenshot files to runtime-level `debug/` directory with collision-safe naming.
- Keep returning the original flow error; screenshot capture failure must not mask the primary error.
- Add/extend automated tests that assert the error-path behavior and screenshot side-effect.
- Include a cleanup/removal note for this temporary side effect.

## Acceptance Criteria
- Tests fail before change and pass after change for the browser-webapi error path.
- Error-path run attempts screenshot capture exactly once per failed flow execution.
- Screenshot file path resolves under runtime `debug/` and file is persisted when capture succeeds.
- If screenshot capture fails, original browser-webapi error still surfaces unchanged.
- Workflow task log includes verification commands and cleanup note.

## Tasks
1. [x] Create/switch to a working branch in all potentially touched repos (main repo + related/submodule repos).
  Done:
  - `embassy-access`: created/switched to `feat/italian-browser-webapi-error-debug-screenshot` from `dev`.
  - `submodules/fsharp-web`: created/switched to `feat/italian-browser-webapi-error-debug-screenshot` from `dev`.
2. [x] Implement screenshot-on-error behavior in the Italian browser-webapi flow, writing artifacts to runtime `debug/`.
  Done:
  - Added `BrowserWebApi.screenshot` in `src/embassy-access-modules/embassy-access-italian/prenotami/Client.fs` and wired it to `BrowserWebApi.Request.Tab.screenshot`.
  - Updated `src/embassy-access-modules/embassy-access-italian/prenotami/Web.BrowserWebApi.fs` to capture screenshot on flow error after tab creation.
  - Implemented runtime `debug/` file persistence with collision-safe route-based naming: `italian-prenotami-route-{route}-{timestamp}-{guid}.png`.
3. [ ] Add/adjust tests for Italian browser-webapi error handling to cover screenshot side-effect behavior.
  Done:
  - (pending)
4. [ ] Ensure screenshot capture is best-effort and does not replace the original browser-webapi error path result.
  Done:
  - (pending)
5. [ ] Add temporary-cleanup note/todo for removing this side effect after debugging period.
  Done:
  - (pending)
6. [ ] Verify with targeted/local test commands and record the executed commands/results.
  Done:
  - (pending)
