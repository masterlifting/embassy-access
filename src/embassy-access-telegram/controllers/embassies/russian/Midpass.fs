[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Midpass

open Infrastructure.Prelude
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Services.Consumer.Embassies.Russian.Midpass
open EA.Telegram.Endpoints.Embassies.Russian.Midpass

let get request =
    fun (deps: Russian.Dependencies) ->
        Midpass.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get.Status number -> Query.checkStatus number
            |> fun createResponse -> deps |> createResponse |> deps.sendResult)
