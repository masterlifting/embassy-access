[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users.Users

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Users
open EA.Telegram.Endpoints.Users.Request
open EA.Telegram.Dependencies
open EA.Telegram.Services.Culture
open EA.Telegram.Services.Users.Service

let respond request chat =
    fun (deps: Request.Dependencies) ->
        Users.Dependencies.create chat deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.Culture
                |> Message.translateRes chat.Culture msgRes

            let sendMessage getResponse =
                deps |> (getResponse >> translate) |> deps.sendMessageRes

            match request with
            | Get get ->
                match get with
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Get.UserEmbassyServices embassyId -> Query.getUserEmbassyServices embassyId
                | Get.UserEmbassyService(embassyId, serviceId) -> Query.getUserEmbassyService embassyId serviceId
                |> sendMessage)
