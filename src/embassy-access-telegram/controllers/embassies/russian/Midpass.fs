[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies.Russian.Midpass

open EA.Telegram.Endpoints.Embassies.Russian.Midpass
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian.Midpass

let get request =
    fun (deps: Russian.Dependencies) ->
        Midpass.Dependencies.create deps
        |> fun deps ->
            match request with
            | Get.Status number -> Query.checkStatus number
            >> deps.translateMessageRes
            >> deps.sendMessageRes
            <| deps
