[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Core

open EA.Telegram.Endpoints.Consumer.Request

let respond request =
    fun deps ->
        match request with
        | Request.Users value -> deps |> Users.respond value
        | Request.Embassies value -> deps |> Embassies.Core.respond value
        | Request.RussianEmbassy value -> deps |> Embassies.Russian.respond value