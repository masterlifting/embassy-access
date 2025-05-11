[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies

open Infrastructure.Prelude
open EA.Telegram.Router.Embassies
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Embassies
open EA.Telegram.Services.Embassies

let respond request chat =
    fun (deps: Request.Dependencies) ->
        deps
        |> Embassies.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Method.Get get ->
                match get with
                | Get.Embassy embassyId -> Query.getEmbassy embassyId
                | Get.Embassies -> Query.getEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                |> fun f -> deps |> f |> deps.sendMessageRes)
