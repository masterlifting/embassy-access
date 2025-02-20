[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Culture.Culture

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Culture
open EA.Telegram.Endpoints.Culture.Request
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Services.Consumer.Culture

let respond request =
    fun (deps: Consumer.Dependencies) ->
        Culture.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.Cultures -> Query.getCultures ()
                | Get.CulturesCallback route -> Query.getCulturesCallback route
                |> fun createResponse -> deps |> createResponse |> deps.sendResult
            | Post post ->
                match post with
                | Post.SetCulture culture -> Command.setCulture culture
                | Post.SetCultureCallback (route, culture) -> Command.setCultureCallback route culture
                |> fun createResponse -> deps |> createResponse |> deps.sendResult)
