module EA.Telegram.Services.Embassies.Russian.Midpass.Query

open EA.Telegram.Domain
open Infrastructure.Domain
open EA.Telegram.Dependencies.Embassies.Russian

let checkStatus (_: string) =
    fun (_: Midpass.Dependencies) ->
        "The Midpass service is not implemented yet " + Constants.NOT_IMPLEMENTED
        |> NotImplemented
        |> Error
        |> async.Return
