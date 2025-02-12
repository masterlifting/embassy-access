[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Controller

open EA.Telegram.Controllers.Consumer.Culture
open EA.Telegram.Controllers.Consumer.Users
open EA.Telegram.Controllers.Consumer.Embassies
open EA.Telegram.Controllers.Consumer.Embassies.Russian
open EA.Telegram.Endpoints.Consumer.Request

let respond route =
    match route with
    | Request.Culture value -> Culture.respond value
    | Request.Users value -> Users.respond value
    | Request.Embassies value -> Embassies.respond value
    | Request.RussianEmbassy value -> Russian.respond value
