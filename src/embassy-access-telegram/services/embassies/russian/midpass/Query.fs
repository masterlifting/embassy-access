module EA.Telegram.Services.Embassies.Russian.Midpass.Query

open EA.Core.Domain
open Infrastructure.Domain
open EA.Telegram.Dependencies.Embassies.Russian

let checkStatus (_: string) =
    fun (_: Midpass.Dependencies) ->
        "The Midpass service is not implemented yet. " + NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return
