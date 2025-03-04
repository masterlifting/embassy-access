[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users.Users

open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Users
open EA.Telegram.Endpoints.Users.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Users.Service
open EA.Telegram.Services.Consumer.Culture

let respond request chat =
    fun (deps: Consumer.Dependencies) ->
        let translate msgRes =
            deps |> Command.translateRes chat.Culture msgRes

        let translateSeq msgSeqRes =
            deps |> Command.translateSeqRes chat.Culture msgSeqRes

        Users.Dependencies.create chat (translate, translateSeq) deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Get.UserEmbassyServices embassyId -> Query.getUserEmbassyServices embassyId
                | Get.UserEmbassyService(embassyId, serviceId) -> Query.getUserEmbassyService embassyId serviceId
                |> fun createResponse -> deps |> (createResponse >> translate) |> deps.sendResult)
