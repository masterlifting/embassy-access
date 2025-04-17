[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Controller

open EA.Telegram.Router
open EA.Telegram.Controllers.Embassies.Russian
open EA.Telegram.Controllers.Embassies.Italian

let rec respond request =

    let useCulture = request |> Culture.apply

    match request with
    | Router.Culture value -> Culture.respond value respond
    | Router.Embassies value -> value |> Embassies.respond |> useCulture
    | Router.Services value -> value |> Russian.respond |> useCulture
    | Router.Subscriptions value -> value |> Italian.respond |> useCulture
