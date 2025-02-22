[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users.Users

open Infrastructure.Prelude
open EA.Telegram.Domain
open EA.Telegram.Endpoints.Users
open EA.Telegram.Endpoints.Users.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Users.Service
open EA.Telegram.Controllers.Consumer.Culture

let respond request chat =
    fun (deps: Consumer.Dependencies) ->
        let applyCulture msg = deps |> Culture.apply chat.Culture msg

        Users.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId >> applyCulture
                | Get.UserEmbassyServices embassyId -> Query.getUserEmbassyServices embassyId
                | Get.UserEmbassyService(embassyId, serviceId) -> Query.getUserEmbassyService embassyId serviceId
                |> fun createResponse -> deps |> createResponse |> deps.sendResult)
