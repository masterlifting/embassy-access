[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Users
open EA.Telegram.Endpoints.Consumer.Users

let respond request =
    fun (deps: Consumer.Dependencies) ->
        let sendResult data =
            Web.Telegram.Producer.produceResult data deps.ChatId deps.CancellationToken deps.TelegramBot
            |> ResultAsync.map ignore

        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | UserEmbassies userId -> deps |> Get.userEmbassies userId |> sendResult
                | UserEmbassy(userId, embassyId) -> deps |> Get.userEmbassy userId embassyId |> sendResult
                | UserEmbassyServices(userId, embassyId) ->
                    deps |> Get.userEmbassyServices userId embassyId |> sendResult
                | UserEmbassyService(userId, embassyId, serviceId) ->
                    deps |> Get.userEmbassyService userId embassyId serviceId |> sendResult)
