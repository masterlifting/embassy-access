[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies.Russian.Russian

open Infrastructure.Prelude
open EA.Telegram.Router.Embassies.Russian
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Embassies.Russian

let respond request chat =
    fun (deps: Request.Dependencies) ->
        Russian.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Method.Get(Get.Kdmid get) -> deps |> Kdmid.get get
            | Method.Get(Get.Midpass get) -> deps |> Midpass.get get)
