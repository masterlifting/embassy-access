[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Controller

open EA.Telegram.Router
open EA.Telegram.Controllers

let rec respond request =

    let useCulture = request |> Culture.apply

    match request with
    | Router.Culture value -> Culture.respond value respond
    | Router.Embassies value -> value |> Embassies.respond |> useCulture
    | Router.Services value -> value |> Services.respond |> useCulture
    | Router.Subscriptions value -> value |> Subscriptions.respond |> useCulture
