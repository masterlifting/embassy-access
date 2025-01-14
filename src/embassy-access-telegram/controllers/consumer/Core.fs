[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Core

open EA.Telegram.Endpoints.Consumer.Request

let respond request =
    fun deps ->
        match request with
        | Route.Users value -> deps |> Users.respond value
        | Route.Embassies value -> deps |> Embassies.Core.respond value
        | Route.RussianEmbassy value -> deps |> Embassies.Russian.respond value