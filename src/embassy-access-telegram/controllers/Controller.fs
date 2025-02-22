[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Controller

open EA.Telegram.Endpoints.Request
open EA.Telegram.Controllers.Consumer.Culture
open EA.Telegram.Controllers.Consumer.Users
open EA.Telegram.Controllers.Consumer.Embassies
open EA.Telegram.Controllers.Consumer.Embassies.Russian

let rec respond request =
    match request with
    | Request.Culture value -> Culture.respond value respond
    | Request.Users value -> Users.respond value |> (Culture.useCulture request)
    | Request.Embassies value -> Embassies.respond value |> (Culture.useCulture request)
    | Request.RussianEmbassy value -> Russian.respond value |> (Culture.useCulture request)
