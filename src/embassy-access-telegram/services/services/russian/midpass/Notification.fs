module EA.Telegram.Services.Services.Russian.Midpass.Notification

open Infrastructure.Domain
open EA.Core.Domain
open EA.Telegram.Dependencies.Services.Russian
open EA.Russian.Services.Domain.Midpass

let spread (request: Request<Payload>) =
    fun (deps: Kdmid.Notification.Dependencies) ->
        $"The spread messages is not implemented yet."
        |> NotImplemented
        |> Error
        |> async.Return
