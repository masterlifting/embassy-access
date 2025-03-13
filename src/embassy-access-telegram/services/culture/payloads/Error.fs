[<RequireQualifiedAccess>]
module EA.Telegram.Services.Culture.Payloads.Error

open Infrastructure.Domain
open Infrastructure.Prelude
open AIProvider.Services.Culture
open Web.Telegram.Domain.Producer
open EA.Telegram.Dependencies.Consumer

let translate culture (error: Error') =
    fun (deps: Culture.Dependencies) ->
        error |> async.Return