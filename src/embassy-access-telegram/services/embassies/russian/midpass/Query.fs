module EA.Telegram.Services.Consumer.Embassies.Russian.Midpass.Query

open Infrastructure.Domain
open EA.Telegram.Dependencies.Consumer.Embassies.Russian

let checkStatus (_: string) =
    fun (_: Midpass.Dependencies) -> "checkStatus" |> NotImplemented |> Error |> async.Return
