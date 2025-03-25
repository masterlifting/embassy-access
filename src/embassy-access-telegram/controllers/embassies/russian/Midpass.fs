[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Consumer.Embassies.Russian.Midpass

open EA.Telegram.Endpoints.Embassies.Russian.Midpass
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Culture
open EA.Telegram.Services.Embassies.Russian.Midpass

let get request =
    fun (deps: Russian.Dependencies) ->
        Midpass.Dependencies.create deps
        |> fun deps ->

            let translate msgRes =
                deps.Culture |> Message.translateRes deps.Chat.Culture msgRes

            let sendMessage getResponse =
                deps |> (getResponse >> translate) |> deps.sendMessageRes

            match request with
            | Get.Status number -> Query.checkStatus number
            |> sendMessage
