[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Users
open EA.Telegram.Endpoints.Consumer.Users

let respond request =
    fun (deps: Core.Dependencies) ->
        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | UserEmbassies userId -> deps |> Get.userEmbassies userId
                | UserEmbassy(userId, embassyId) -> deps |> Get.userEmbassy userId embassyId
                | UserEmbassyServices(userId, embassyId) -> deps |> Get.userEmbassyServices userId embassyId
                | UserEmbassyService(userId, embassyId, serviceId) ->
                    deps |> Get.userEmbassyService userId embassyId serviceId)
