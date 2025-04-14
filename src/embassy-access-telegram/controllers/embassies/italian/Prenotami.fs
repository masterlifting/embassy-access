[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies.Italian.Prenotami

open EA.Telegram.Router.Embassies.Italian.Prenotami
open EA.Telegram.Dependencies.Embassies.Italian
open EA.Telegram.Services.Embassies.Italian.Prenotami

let get request =
    fun (deps: Italian.Dependencies) ->
        Prenotami.Dependencies.create deps
        |> fun deps ->
            match request with
            | Get.Appointments requestId -> Query.getAppointments requestId
            | Get.SubscriptionsMenu requestId -> Query.getSubscriptionsMenu requestId
            >> deps.translateMessageRes
            >> deps.sendMessageRes
            <| deps
