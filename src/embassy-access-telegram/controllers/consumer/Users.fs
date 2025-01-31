﻿[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Users

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer.Users
open EA.Telegram.Services.Consumer.Users.Service
open EA.Telegram.Endpoints.Consumer.Users.Request

let respond request =
    fun (deps: Consumer.Dependencies) ->
        Users.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Get.UserEmbassyServices embassyId -> Query.getUserEmbassyServices embassyId
                | Get.UserEmbassyService(embassyId, serviceId) -> Query.getUserEmbassyService embassyId serviceId
                |> fun createResponse -> deps |> createResponse |> deps.sendResult)
