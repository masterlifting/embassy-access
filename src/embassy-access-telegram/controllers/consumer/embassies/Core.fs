[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Core

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Embassies.Core
open EA.Telegram.Endpoints.Consumer.Embassies.Core

let respond request =
    fun (deps: Core.Dependencies) ->
        Embassies.Core.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Embassies -> deps |> Get.embassies
                | Embassy embassyId -> deps |> Get.embassy embassyId
                | EmbassyServices embassyId -> deps |> Get.embassyServices embassyId
                | EmbassyService(embassyId, serviceId) -> deps |> Get.embassyService embassyId serviceId)
