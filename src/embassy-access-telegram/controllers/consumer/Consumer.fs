[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Consumer

open EA.Telegram.Endpoints.Consumer.Request

let respond request =
    fun deps ->
        match request with
        | Route.Users value -> deps |> Users.respond value
        | Route.Embassies value -> deps |> Embassies.Embassies.respond value
        | Route.RussianEmbassy value -> deps |> Embassies.RussianEmbassy.respond value