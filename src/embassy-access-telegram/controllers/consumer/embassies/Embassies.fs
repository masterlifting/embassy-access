[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Embassies

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Services.Consumer.Embassies.Service
open EA.Telegram.Endpoints.Consumer.Embassies
open EA.Telegram.Endpoints.Consumer.Embassies.Request

let respond request =
    fun (deps: Consumer.Dependencies) ->
        Embassies.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.Embassies -> Get.embassies ()
                | Get.Embassy embassyId -> Get.embassy embassyId
                | Get.EmbassyServices embassyId -> Get.embassyServices embassyId
                | Get.EmbassyService(embassyId, serviceId) -> Get.embassyService embassyId serviceId
                |> fun createResponse -> deps |> createResponse |> deps.sendResult)
