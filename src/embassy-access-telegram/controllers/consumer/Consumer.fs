[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Consumer

open EA.Telegram.Controllers.Consumer.Embassies
open EA.Telegram.Endpoints.Consumer.Router

let respond route =
    fun deps ->
        match route with
        | Request.Users value -> deps |> Users.respond value
        | Request.Embassies value -> deps |> Embassies.respond value
        | Request.RussianEmbassy value -> deps |> RussianEmbassy.respond value
