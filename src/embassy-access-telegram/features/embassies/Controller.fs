[<RequireQualifiedAccess>]
module EA.Telegram.Features.Embassies.Controller

open Infrastructure.Prelude
open EA.Telegram.Features.Dependencies
open EA.Telegram.Features.Embassies.Services
open EA.Telegram.Features.Embassies.Router
open EA.Telegram.Dependencies

let respond request chat =
    fun (deps: Request.Dependencies) ->
        deps
        |> Embassies.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Method.Get get ->
                match get with
                | Get.Embassy embassyId -> Query.getEmbassy embassyId
                | Get.Embassies -> Query.getEmbassies ()
                | Get.UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | Get.UserEmbassies -> Query.getUserEmbassies ()
                |> fun f -> deps |> f |> deps.sendMessageRes)
