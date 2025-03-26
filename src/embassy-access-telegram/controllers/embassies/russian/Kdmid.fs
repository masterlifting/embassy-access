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

let post request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->

            match request with
            | Post.Subscribe model -> Command.subscribe model >> deps.translateMessageRes >> deps.sendMessageRes
            | Post.CheckAppointments model ->
                Command.checkAppointments model
                >> deps.translateMessageRes
                >> deps.sendMessageRes
            | Post.SendAppointments model ->
                Command.sendAppointments model
                >> deps.translateMessagesRes
                >> deps.sendMessagesRes
            | Post.ConfirmAppointment model ->
                Command.confirmAppointment model
                >> deps.translateMessageRes
                >> deps.sendMessageRes
            |> fun send -> deps |> send)

let delete request =
    fun (deps: Russian.Dependencies) ->
        Kdmid.Dependencies.create deps
        |> ResultAsync.wrap (fun deps ->
            match request with
            | Delete.Subscription requestId -> Command.deleteSubscription requestId
            >> deps.translateMessageRes
            >> deps.sendMessageRes
            <| deps)
