[<RequireQualifiedAccess>]
module EA.Telegram.Controllers.Embassies.Russian.Kdmid

open Infrastructure.Prelude
open EA.Telegram.Router.Embassies.Russian.Kdmid
open EA.Telegram.Dependencies.Embassies.Russian
open EA.Telegram.Services.Embassies.Russian.Kdmid

let get request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Get.Appointments requestId -> Query.getAppointments requestId
            | Get.SubscriptionsMenu requestId -> Query.getSubscriptionsMenu requestId
            >> deps.translateMessageRes
            >> deps.sendMessageRes
            <| deps)
