[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Controller

open EA.Telegram.Endpoints.Request
open EA.Telegram.Controllers.Consumer.Culture
open EA.Telegram.Controllers.Consumer.Users
open EA.Telegram.Controllers.Consumer.Embassies
open EA.Telegram.Controllers.Consumer.Embassies.Russian

let rec respond request =

    let useCulture = request |> Culture.apply

    match request with
    | Culture value -> Culture.respond value respond
    | Users value -> value |> Users.respond |> useCulture
    | Embassies value -> value |> Embassies.respond |> useCulture
    | RussianEmbassy value -> value |> Russian.respond |> useCulture
