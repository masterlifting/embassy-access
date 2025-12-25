[<RequireQualifiedAccess>]
module EA.Telegram.Features.Controller.Embassies

open Infrastructure.Prelude
open EA.Telegram.Features.Dependencies
open EA.Telegram.Features.Services.Embassies
open EA.Telegram.Router.Embassies
open EA.Telegram.Dependencies

let respond request chat =
    fun (deps: Request.Dependencies) ->
        deps
        |> Embassies.Dependencies.create chat
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Embassy embassyId -> Query.getEmbassy embassyId
                | Embassies -> Query.getEmbassies ()
                | UserEmbassy embassyId -> Query.getUserEmbassy embassyId
                | UserEmbassies -> Query.getUserEmbassies ()
                |> fun f -> deps |> f |> deps.sendMessageRes)
