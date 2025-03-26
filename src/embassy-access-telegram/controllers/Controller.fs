[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Controller

open EA.Telegram.Endpoints.Request
open EA.Telegram.Controllers.Embassies
open EA.Telegram.Controllers.Embassies.Russian

let rec respond request =

    let useCulture = request |> Culture.apply

    match request with
    | Culture value -> Culture.respond value respond
    | Users value -> value |> Users.respond |> useCulture
    | Embassies value -> value |> Embassies.respond |> useCulture
    | RussianEmbassy value -> value |> Russian.respond |> useCulture
