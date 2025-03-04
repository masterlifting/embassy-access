[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Embassies

open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Embassies
open EA.Telegram.Endpoints.Embassies.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Services.Consumer.Embassies.Service
open EA.Telegram.Services.Consumer.Culture

let respond request chat =
    fun (deps: Consumer.Dependencies) ->
        let translate msgRes =
            deps |> Command.translateRes chat.Culture msgRes

        let translateSeq msgSeqRes =
            deps |> Command.translateSeqRes chat.Culture msgSeqRes

        Embassies.Dependencies.create chat (translate, translateSeq) deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.Embassies -> Query.getEmbassies ()
                | Get.Embassy embassyId -> Query.getEmbassy embassyId
                | Get.EmbassyServices embassyId -> Query.getEmbassyServices embassyId
                | Get.EmbassyService(embassyId, serviceId) -> Query.getEmbassyService embassyId serviceId
                |> fun createResponse -> deps |> (createResponse >> translate) |> deps.sendResult)
