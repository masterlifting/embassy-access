[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Controller

open EA.Telegram.Endpoints.Request
open EA.Telegram.Controllers.Consumer.Culture
open EA.Telegram.Controllers.Consumer.Users
open EA.Telegram.Controllers.Consumer.Embassies
open EA.Telegram.Controllers.Consumer.Embassies.Russian

let rec respond request =
    match request with
    | Culture value -> Culture.respond value respond
    | Users value -> value |> Users.respond |> (Culture.apply request)
    | Embassies value -> value |> Embassies.respond |> (Culture.apply request)
    | RussianEmbassy value -> value |> Russian.respond |> (Culture.apply request)
