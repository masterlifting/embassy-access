[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Embassies

open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Embassies
open EA.Telegram.Endpoints.Embassies.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Services
open EA.Telegram.Services.Consumer.Embassies.Service

let respond request chat =
    fun (deps: Request.Dependencies) ->
        Embassies.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture.toProducer ()
                |> Producer.Culture.Command.translateRes chat.Culture msgRes

            let sendResult getResponse =
                deps |> (getResponse >> translate) |> deps.sendResult

            match request with
            | Get get ->
                match get with
                | Get.Embassies -> Query.getEmbassies ()
                | Get.Embassy embassyId -> Query.getEmbassy embassyId
                | Get.EmbassyServices embassyId -> Query.getEmbassyServices embassyId
                | Get.EmbassyService(embassyId, serviceId) -> Query.getEmbassyService embassyId serviceId
                |> sendResult)
