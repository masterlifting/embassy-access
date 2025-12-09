[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Controller

open EA.Telegram.Router

module Culture = EA.Telegram.Features.Culture.Controller
module Embassies = EA.Telegram.Features.Embassies.Controller
module Services = EA.Telegram.Features.Services.Controller

let rec respond request =

    let useCulture = request |> Culture.apply

    match request with
    | Router.Culture value -> Culture.respond value respond
    | Router.Embassies value -> value |> Embassies.respond |> useCulture
    | Router.Services value -> value |> Services.respond |> useCulture
