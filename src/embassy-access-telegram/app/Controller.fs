[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Controller

open EA.Telegram.App
open EA.Telegram.Features.Controller

let rec respond (request: Router.Route) =

    let useCulture = request.Value |> Culture.apply

    match request with
    | Router.Culture value ->
        let entripoint = fun value deps -> deps |> respond (Router.Culture value)
        Culture.respond value entripoint
    | Router.Embassies value -> value |> Embassies.respond |> useCulture
    | Router.Services value -> value |> Services.respond |> useCulture
