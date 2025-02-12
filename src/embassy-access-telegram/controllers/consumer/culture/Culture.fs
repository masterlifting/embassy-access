[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Culture.Culture

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer
open EA.Telegram.Endpoints.Consumer.Culture
open EA.Telegram.Services.Consumer.Culture.Service
open EA.Telegram.Endpoints.Consumer.Culture.Request

let respond request =
    fun (deps: Consumer.Dependencies) ->
        Culture.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get get ->
                match get with
                | Get.Cultures -> Query.getCultures ()
                |> fun createResponse -> deps |> createResponse |> deps.sendResult)