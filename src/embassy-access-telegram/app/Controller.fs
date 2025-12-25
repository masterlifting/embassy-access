[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Controller

open EA.Telegram.Router
open EA.Telegram.Features.Controller

let rec respond (request: Route) =

    let useCulture = request |> Culture.apply

    match request with
    | Culture value -> Culture.respond value respond
    | Embassies value -> value |> Embassies.respond |> useCulture
    | Services value -> value |> Services.respond |> useCulture
