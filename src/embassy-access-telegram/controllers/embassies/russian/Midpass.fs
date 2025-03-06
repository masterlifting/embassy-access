[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Midpass

open Infrastructure.Prelude
open EA.Telegram.Endpoints.Embassies.Russian.Midpass
open EA.Telegram.Dependencies.Consumer.Embassies.Russian
open EA.Telegram.Services.Culture
open EA.Telegram.Services.Consumer.Embassies.Russian.Midpass

let get request =
    fun (deps: Russian.Dependencies) ->
        Midpass.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            let translate msgRes =
                deps.CultureDeps |> Command.translateRes deps.Chat.Culture msgRes

            let sendResult getResponse =
                deps |> (getResponse >> translate) |> deps.sendResult

            match request with
            | Get.Status number -> Query.checkStatus number
            |> sendResult)
