[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users.Users

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Users
open EA.Telegram.Endpoints.Users.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services
open EA.Telegram.Services.Consumer.Users.Service

let respond request chat =
    fun (deps: Request.Dependencies) ->
        Users.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture
                |> Producer.Culture.Command.translateRes chat.Culture msgRes deps.Culture.Placeholder

            let sendResult getResponse =
                deps |> (getResponse >> translate) |> deps.sendResult

            match request with
            | Get get ->
                match get with
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Get.UserEmbassyServices embassyId -> Query.getUserEmbassyServices embassyId
                | Get.UserEmbassyService(embassyId, serviceId) -> Query.getUserEmbassyService embassyId serviceId
                |> sendResult)
