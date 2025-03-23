[<RequireQualifiedAccess>]
module EA.Telegram.Services.Culture.Command

open Infrastructure.Domain
open Infrastructure.Prelude
open EA.Telegram.Dependencies
open EA.Telegram.Services.Culture.Payloads

let translateError culture error =
    fun (deps: Culture.Dependencies) ->
        deps
        |> Error.translate culture error
        |> Async.map (function
            | Ok error -> error
            | Error error -> error)
