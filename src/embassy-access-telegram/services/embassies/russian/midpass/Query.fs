module EA.Telegram.Services.Embassies.Russian.Midpass.Query

open Infrastructure.Domain
open EA.Telegram.Dependencies.Embassies.Russian

let checkStatus (_: string) =
    fun (_: Midpass.Dependencies) -> "Midpass checkStatus" |> NotImplemented |> Error |> async.Return
