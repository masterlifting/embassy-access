## Pattern: Chaining BrowserWebApi Steps With ResultAsyncBuilder

In this repo, `ResultAsyncBuilder` (see `submodules/fsharp-infrastructure/src/prelude/ComputationExpression.fs`) binds `Async<Result<_, Error'>>` using `ResultAsync.bindAsync`.

### Recommended shape
- Model running state as a context value (e.g. `{ HttpClient; TabId }`).
- Make each step return `Async<Result<TabCtx, Error'>>` so it composes with `let!`/`do!`.
- For steps returning `unit`, `map (fun () -> ctx)` to keep the pipeline type stable.
- For “optional” steps (skip on a specific known error), create a small wrapper that converts that error into `Ok ctx`.

### Example
```fsharp
type TabCtx = { HttpClient: Http.Client; TabId: string }
let resultAsync = ResultAsyncBuilder()

let openHomePage (api: BrowserWebApi) (http: Http.Client) : Async<Result<TabCtx, Error'>> =
  http
  |> api.openTab { Dto.Open.Url = "https://prenotami.esteri.it"; Expiration = 120UL }
  |> ResultAsync.map (fun tabId -> { HttpClient = http; TabId = tabId })

let fillCredentials (creds: Credentials) (api: BrowserWebApi) (ctx: TabCtx) : Async<Result<TabCtx, Error'>> =
  ctx.HttpClient
  |> api.fillCredentials ctx.TabId { Dto.Fill.Inputs = [...] }
  |> ResultAsync.map (fun () -> ctx)

let fillCredentialsOrSkip creds api ctx =
  async {
    match! fillCredentials creds api ctx with
    | Ok ctx -> return Ok ctx
    | Error e when e.Message.Contains "#login-email" -> return Ok ctx
    | Error e -> return Error e
  }

let processWebSite creds serviceId (api: BrowserWebApi) : Async<Result<string, Error'>> =
  resultAsync {
    let! http = api.init () |> async.Return
    let! ctx = http |> openHomePage api
    let! ctx = ctx |> fillCredentialsOrSkip creds api
    let! ctx = ctx |> submitCaptcha api
    let! ctx = ctx |> submitCredentials api
    let! ctx = ctx |> clickBookService api
    let! ctx = ctx |> clickBookAppointment serviceId api
    let! result = ctx |> extractResult api
    do! ctx |> closeTab api |> ResultAsync.map ignore
    return result
  }
```
