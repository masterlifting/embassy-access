[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Consumer

open EA.Telegram.Controllers.Consumer.Embassies
open EA.Telegram.Endpoints.Consumer.Router

let respond route =
    match route with
    | Request.Users value -> Users.respond value
    | Request.Embassies value -> Embassies.respond value
    | Request.RussianEmbassy value -> RussianEmbassy.respond value
