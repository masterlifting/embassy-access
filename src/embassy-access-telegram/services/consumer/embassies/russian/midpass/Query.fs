module EA.Telegram.Services.Consumer.Embassies.Russian.Midpass.Query

open Infrastructure.Domain
open EA.Telegram.Dependencies.Consumer.Embassies

let checkStatus (_: string) =
    fun (_: RussianEmbassy.Dependencies) -> "checkStatus" |> NotImplemented |> Error |> async.Return
