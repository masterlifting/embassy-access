## Pattern: Chaining `Async<Result<_,_>>` With `ResultAsyncBuilder`

In this repo, `ResultAsyncBuilder` is a computation expression for workflows shaped like `Async<Result<'T, 'E>>`.

- `Bind` uses `ResultAsync.bindAsync`, so `let! x = ...` expects an `Async<Result<_,_>>` and short-circuits on `Error`.
- **Important:** `Return`/`ReturnFrom` are also defined in terms of `Async<Result<_,_>>` (not plain `'T`).
  - Inside `resultAsync { ... }`, `return` should produce an `Async<Result<_,_>>`.

### Minimal helpers (recommended)
Use small helpers locally (or in a shared module) to avoid repeating `async.Return (Ok ...)`:

```fsharp
type AsyncResult<'t,'e> = Async<Result<'t,'e>>

let ok (x: 't) : AsyncResult<'t,'e> = async.Return (Ok x)
let error (e: 'e) : AsyncResult<'t,'e> = async.Return (Error e)
```

### Step chaining pattern
Make each step return `Async<Result<_,_>>` so it composes with `let!`/`do!`.

Two common shapes:

1) Keep state explicit as a context value (record/tuple) and return it from each step:

```fsharp
let step1 (ctx: Ctx) : Async<Result<Ctx, Err>> = ...
let step2 (ctx: Ctx) : Async<Result<Ctx, Err>> = ...
let stepUnit (ctx: Ctx) : Async<Result<unit, Err>> = ...

resultAsync {
  let! ctx = step1 ctx0
  let! ctx = step2 ctx
  do! stepUnit ctx
  return! ok ctx
}
```

2) If you already have the state split across arguments (e.g. `httpClient` + `tabId`), keep passing them, but keep the return type consistent (`Async<Result<_,_>>`).

### Converting pure `Result` into the async pipeline
When you have a pure `Result<'a,'e>` and want to continue with an async step, use `ResultAsync.wrap`:

```fsharp
let next (a: 'a) : Async<Result<'b,'e>> = ...

resultAsync {
  let! b =
    somePureResult
    |> ResultAsync.wrap next

  return! ok b
}
```

### Optional/ignorable errors
If a step can be skipped for a specific known error, wrap it so that error becomes `Ok ctx` (and other errors still fail):

```fsharp
let stepOrSkip (ctx: Ctx) : Async<Result<Ctx, Err>> =
  async {
    match! step ctx with
    | Ok ctx -> return Ok ctx
    | Error e when shouldSkip e -> return Ok ctx
    | Error e -> return Error e
  }
```

### Cleanup
Prefer doing cleanup as the last `do!` in the same `resultAsync { ... }` block.
If cleanup should not fail the overall result, explicitly swallow its error in a small wrapper (instead of letting it short-circuits).
