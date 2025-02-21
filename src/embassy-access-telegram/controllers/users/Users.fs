[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users.Users

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Users
open EA.Telegram.Endpoints.Users.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Users.Service

let respond request chat =
    fun (deps: Consumer.Dependencies) ->
        Users.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Get.UserEmbassyServices embassyId -> Query.getUserEmbassyServices embassyId
                | Get.UserEmbassyService(embassyId, serviceId) -> Query.getUserEmbassyService embassyId serviceId
                |> fun createResponse -> deps |> createResponse |> deps.sendResult)
