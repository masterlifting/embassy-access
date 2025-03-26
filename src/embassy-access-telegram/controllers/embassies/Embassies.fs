[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies.Embassies

open Infrastructure.Prelude
open EA.Telegram.Router.Embassies
open EA.Telegram.Dependencies
open EA.Telegram.Dependencies.Embassies
open EA.Telegram.Services.Embassies

let respond request chat =
    fun (deps: Request.Dependencies) ->
        Embassies.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Method.Get get ->
                match get with
                | Get.Embassies -> Query.getEmbassies ()
                | Get.Embassy embassyId -> Query.getEmbassy embassyId
                | Get.EmbassyServices embassyId -> Query.getEmbassyServices embassyId
                | Get.EmbassyService(embassyId, serviceId) -> Query.getEmbassyService embassyId serviceId
                >> deps.translateMessageRes
                >> deps.sendMessageRes
                <| deps)
