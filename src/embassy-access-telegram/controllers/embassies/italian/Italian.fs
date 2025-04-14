[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies.Italian.Italian

open Infrastructure.Prelude
open EA.Telegram.Router.Embassies.Italian
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Embassies.Italian

let respond request chat =
    fun (deps: Request.Dependencies) ->
        Italian.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Method.Get(Get.Prenotami get) -> deps |> Prenotami.get get)
