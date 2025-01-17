[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Embassies

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Dependencies.Consumer.Embassies
open EA.Telegram.Services.Consumer.Embassies.Embassies
open EA.Telegram.Endpoints.Consumer.Embassies.Embassies

let respond request =
    fun (deps: Consumer.Dependencies) ->
        let sendResult data =
            Web.Telegram.Producer.produceResult data deps.ChatId deps.CancellationToken deps.TelegramBot
            |> ResultAsync.map ignore

        Embassies.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Embassies -> deps |> Get.embassies |> sendResult
                | Embassy embassyId -> deps |> Get.embassy embassyId |> sendResult
                | EmbassyServices embassyId -> deps |> Get.embassyServices embassyId |> sendResult
                | EmbassyService(embassyId, serviceId) -> deps |> Get.embassyService embassyId serviceId |> sendResult)
